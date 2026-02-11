using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore.Contracts.Dcp;
using ViciOne.Driver.IoTCore.DcpScan;

namespace ifm.IOLink;

public sealed partial class IoLinkMasterFinder
{
    private const string IoLinkMasterIdentifier = "ifm IO-Link Master AL";

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _runningTask = Task.CompletedTask;
    private int _scanRunning;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // TODO: CancellationToken im Callback des Tasks beachten und korrekt auf Beendigung warten
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

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if (ProcessDataInputs.Trigger.Changed &&
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
            devices = [.. devices.Where(x => x.DeviceName.StartsWith(IoLinkMasterIdentifier, StringComparison.OrdinalIgnoreCase))];

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
            response = await httpClient.GetAsync(new Uri(device.Address, "getidentity"), cancellationToken);
            isUnreachable = !(response?.IsSuccessStatusCode ?? false);
        }
        catch (HttpRequestException httpEx)
        {
            Logger.LogInformation($"Fetching info of device {device.Ip} failed: {httpEx.StatusCode}.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Error trying to fetch info of device {device.Ip}: {ex.GetType().Name} {ex.Message} {ex.StackTrace}.");
        }
        finally
        {
            response?.Dispose();
        }
        device.IsUnreachable = isUnreachable;
    }
}
