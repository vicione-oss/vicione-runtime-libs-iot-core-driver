using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Communication.IoTCore.DcpScan;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts.Scanning;

namespace ifm.IOLink;

public sealed partial class IoLinkMasterScanner
{
    private const string IoLinkMasterIdentifier = "ifm IO-Link Master AL";

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _runningTask = Task.CompletedTask;
    private int _scanRunning;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // TODO: honor the CancellationToken in the task's callback and wait correctly for completion
            _cancellationTokenSource.Cancel();

            try
            {
                _runningTask.GetAwaiter().GetResult();
                _runningTask.Dispose();
            }
            catch (InvalidOperationException) { }

            _cancellationTokenSource.Dispose();
        }
    }

    /// <inheritdoc/>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if ((ProcessDataInputs.Trigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.Trigger.Value &&
            _runningTask.Status != TaskStatus.WaitingForActivation)
        {
            ScanIfPossible();
        }
    }

    private void ScanIfPossible()
    {
        if (Interlocked.CompareExchange(ref _scanRunning, 1, 0) != 0)
            return;

        _runningTask = Task.Run(async () =>
        {
            try
            {
                SetConnectorValueIfNotEqual(ProcessDataOutputs.Devices, await Scanning(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Logger.ScanningErrorOccurred(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _scanRunning, 0);
            }
        }, _cancellationTokenSource.Token);
    }

    private async Task<DcpScanningResult> Scanning(CancellationToken cancellationToken)
    {
        DcpScanningResult scanningResult = new();
        try
        {
            DcpScanner scanner = new(new()
            {
                IncludeLoopbacks = false,
                SendRetries = 5,
                AttemptWindow = TimeSpan.FromMilliseconds(200),
                StragglerWindow = TimeSpan.FromSeconds(5),
            }, Logger);

            using CancellationTokenSource timeoutToken = new(TimeSpan.FromSeconds(30));
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token);

            List<DcpDevice> devices = await scanner.Scan(linkedToken.Token);
            devices = [.. devices.Where(x => x.Identity.Name.StartsWith(IoLinkMasterIdentifier, StringComparison.OrdinalIgnoreCase))];

            using var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15) // Recreate every 15 minutes
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var httpTasks = new List<Task>();

            foreach (DcpDevice device in devices)
                httpTasks.Add(CheckHttpConnectivityAsync(device, httpClient, cancellationToken));

            await Task.WhenAll(httpTasks);
            scanningResult.Devices.AddRange(devices);
        }
        catch (Exception ex)
        {
            Logger.GenericScanningWarningOccurred(ex);
            scanningResult.Messages.Add(ex.Message);
        }

        return scanningResult;
    }

    private async Task CheckHttpConnectivityAsync(DcpDevice device, HttpClient httpClient, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null!;
        bool isUnreachable = true;

        try
        {
            response = await httpClient.GetAsync(new Uri(device.Network.Address, "getidentity"), cancellationToken);
            isUnreachable = !(response?.IsSuccessStatusCode ?? false);

            if (isUnreachable is false)
                device.Security = await GetSecurityInfoAsync(response!, cancellationToken);
        }
        catch (HttpRequestException httpEx)
        {
            Logger.DeviceInfoFetchFailed(device.Network.Ip, httpEx.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.DeviceInfoFetchErrorOccurred(ex, device.Network.Ip);
        }
        finally
        {
            response?.Dispose();
        }
        device.IsUnreachable = isUnreachable;
    }

    /// <summary>
    /// Reads <paramref name="identityResponse"/>'s body and extracts its <c>security</c> block, if any - not
    /// every IoTCore firmware reports one (older ones just omit it entirely).
    /// </summary>
    /// <param name="identityResponse">The successful response from a <c>getidentity</c> request.</param>
    /// <param name="cancellationToken">Cancellation token for reading the response body.</param>
    /// <returns>The device's security info - every property keeps its default if no <c>security</c> block was present.</returns>
    private static async Task<DcpDeviceSecurity> GetSecurityInfoAsync(HttpResponseMessage identityResponse, CancellationToken cancellationToken)
    {
        string content = await identityResponse.Content.ReadAsStringAsync(cancellationToken);
        JsonNode? data = JsonNode.Parse(content)?["data"];

        // Firmware differs in both where the security block sits and how it names its fields: newer firmware nests
        // it under "iot" with lowercase fields (passwordstatus/securitymode/authscheme); older firmware puts it at
        // the data root with camelCase fields (securityMode/authScheme) and reports "isPasswdSet" instead of
        // "passwordstatus". Older firmware without any security block keeps the defaults.
        JsonNode? security = data?["security"] ?? data?["iot"]?["security"];
        if (security is null)
            return new DcpDeviceSecurity();

        return new DcpDeviceSecurity
        {
            RequiresAuthentication =
                string.Equals(Read(security, "passwordstatus"), "required", StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(Read(security, "securitymode", "securityMode"), "enabled", StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(Read(security, "isPasswdSet"), "true", StringComparison.OrdinalIgnoreCase)),
            AuthenticationScheme = Read(security, "authscheme", "authScheme"),
            SecurityMode = Read(security, "securitymode", "securityMode"),
        };

        static string? Read(JsonNode node, params string[] keys)
        {
            foreach (string key in keys)
                if (node[key] is JsonNode value)
                    return value.GetValue<string>();
            return null;
        }
    }
}
