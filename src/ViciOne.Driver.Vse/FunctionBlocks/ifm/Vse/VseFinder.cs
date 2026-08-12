using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFM.SDK.Scanner;
using IFM.SDK.VSE.Scanner;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts.Scanning;

namespace ifm.Vse;

public sealed partial class VseFinder
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _runningTask = Task.CompletedTask;
    private int _scanRunning;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
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
                SetConnectorValueIfNotEqual(ProcessDataOutputs.Devices, await ScanningAsync(_cancellationTokenSource.Token));
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

    private async Task<VseScanningResult> ScanningAsync(CancellationToken cancellationToken)
    {
        VseScanningResult scanningResult = new();
        try
        {
            IDeviceScanner scanner = new DeviceScannerBuilder().VSEDeviceScanner().WithLocalLan().Build();

            using CancellationTokenSource timeoutToken = new(TimeSpan.FromSeconds(30));
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token);

            List<DeviceInfo> devices = await scanner.ScanAsync(new Progress<int>()).WaitAsync(linkedToken.Token);

            scanningResult.Devices.AddRange(devices.Select(device => new VseScanDevice
            {
                DeviceType = device.DeviceType.ToString(),
                FirmwareVersion = device.FirmwareVersion,
                Hardware = device.Hardware,
                HostName = device.HostName,
                IpAddress = device.IPAddress,
                MacAddress = device.MACAddress,
                Port = device.Port,
            }));
        }
        catch (Exception ex)
        {
            Logger.GenericScanningWarningOccurred(ex);
            scanningResult.Messages.Add(ex.Message);
        }

        return scanningResult;
    }
}
