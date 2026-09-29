using System.Diagnostics;
using IFM.SDK.VSE;
using IFM.SDK.VSE.Monitoring;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;
using Moq;
using ViciOne.Communication.Vse.Data;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Tests;

public class RawDataRecorderTests
{
    private sealed class ReceiverBox
    {
        public Action<IRawDataSample>? Receiver;
    }

    private static (Mock<IVSEDevice> Device, DeviceEventManager EventManager, ReceiverBox ReceiverBox, Mock<ICommands> Commands) CreateDevice(SystemMode systemMode = SystemMode.Supervise, int dynamicInputCount = 4, bool reachesMeasureMode = true)
    {
        // The real device firmware switches into Measure mode once a raw-data monitoring channel is started -
        // simulate that here so StartRecording()'s "wait for Measure mode" loop resolves immediately
        // instead of spinning for its full 5 second timeout. reachesMeasureMode: false simulates a device that
        // never confirms the switch, so the wait loop spins for its full duration.
        SystemMode currentMode = systemMode;
        ReceiverBox receiverBox = new();
        Mock<IMonitoringChannel<IRawDataSample>> channel = new();
        channel.Setup(c => c.Start(It.IsAny<Action<IRawDataSample>>())).Callback<Action<IRawDataSample>>(r =>
        {
            receiverBox.Receiver = r;
            if (systemMode == SystemMode.Supervise && reachesMeasureMode)
                currentMode = SystemMode.Measure;
        });

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<IRawDataSample>(It.IsAny<RawChannelInfo>())).Returns(channel.Object);

        Mock<ICommands> commands = new();
        commands.Setup(c => c.SetSystemMode(It.IsAny<SystemMode>())).Callback<SystemMode>(mode => currentMode = mode);

        // RawDataRecorder derives its sensor count from the device's Dynamic inputs - fake exactly
        // dynamicInputCount of them so sensor indices 1..dynamicInputCount behave like they did with the old
        // hardcoded MaxSensor = 4.
        List<IInput> dynamicInputs = [];
        for (int i = 0; i < dynamicInputCount; i++)
        {
            Mock<IInput> input = new();
            input.Setup(x => x.Type).Returns(InputType.Dynamic);
            dynamicInputs.Add(input.Object);
        }
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Inputs).Returns(dynamicInputs);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.DataLink).Returns(dataLink.Object);
        device.Setup(d => d.SystemMode).Returns(() => currentMode);
        device.Setup(d => d.ConnectionStatus).Returns(ConnectionStatus.Connected);
        device.Setup(d => d.Commands).Returns(commands.Object);
        device.Setup(d => d.Parameters).Returns(parameters.Object);

        DeviceEventManager eventManager = new(device.Object, loggerFactory: null);

        return (device, eventManager, receiverBox, commands);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.Now > deadline)
                throw new TimeoutException("Condition was not met in time.");

            await Task.Delay(10);
        }
    }

    [Fact]
    public void StartRecording_SensorOutOfRange_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.StartRecording(sensor: 5, sampleRate: 50_000, duration: 100));
    }

    [Fact]
    public void StartRecording_SensorCountMatchesDevicesDynamicInputs_DoesNotThrowWithinRangeAndThrowsAboveIt()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice(dynamicInputCount: 2);
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.StartRecording(sensor: 3, sampleRate: 50_000, duration: 100));
        // Sensor 2 is valid for a 2-sensor device - if this throws, the sensor count wasn't actually derived
        // from the device's Dynamic inputs.
        sut.StartRecording(sensor: 2, sampleRate: 50_000, duration: 100, waitUntilFinished: false);
        Assert.True(sut.IsRecording);
    }

    [Fact]
    public void StartRecording_UnsupportedSampleRate_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.StartRecording(sensor: 1, sampleRate: 12_345, duration: 100));
    }

    [Fact]
    public void StartRecording_DurationBelowMinimum_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 50));
    }

    [Fact]
    public void StartRecording_WrongSystemMode_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice(systemMode: SystemMode.Measure);
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);

        Assert.Throws<InvalidOperationException>(() => sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100));
    }

    [Fact]
    public void StartRecording_AlreadyRecording_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);
        sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100, waitUntilFinished: false);

        Assert.Throws<InvalidOperationException>(() => sut.StartRecording(sensor: 2, sampleRate: 50_000, duration: 100));
    }

    [Fact]
    public async Task StopRecording_WhileStillWaitingForMeasureMode_DoesNotBlockBehindThatWait()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice(reachesMeasureMode: false);
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);

        // The device never confirms Measure mode, so this spins in its unlocked up-to-5-second wait loop in the
        // background - StopRecording() below must not be blocked behind that (same _lock) for anywhere
        // near that long.
        _ = Task.Run(() => sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100, waitUntilFinished: false), TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.IsRecording);

        var stopwatch = Stopwatch.StartNew();
        // No recording task exists yet at this point (Measure mode hasn't been confirmed), so stopping fails
        // fast instead of succeeding - the point being tested is that it fails fast rather than blocking.
        Assert.Throws<TimeoutException>(sut.StopRecording);
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"StopRecording took {stopwatch.ElapsedMilliseconds} ms - expected it to return quickly instead of blocking behind the Measure-mode wait.");
    }

    [Fact]
    public async Task StartRecording_ReceivesAllSamples_FinishesSuccessfully()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, ReceiverBox receiverBox, Mock<ICommands> commands) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);
        int stateChanges = 0;
        sut.StateChanged += () => stateChanges++;

        // duration(ms) * sampleRate / 1000 = 100 * 50_000 / 1000 = 5000 samples to fill the buffer.
        sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100, waitUntilFinished: false);
        Assert.True(sut.IsRecording);

        Mock<IRawDataSample> sample = new();
        sample.Setup(s => s.GetValues()).Returns([.. Enumerable.Repeat(1f, 5000)]);
        receiverBox.Receiver!.Invoke(sample.Object);

        await WaitUntilAsync(() => !sut.IsRecording);

        Assert.Equal(5000, sut.BlobSize(1));
        Assert.NotNull(sut.RecordingStartTimeUtc(1));
        Assert.True(stateChanges >= 2);
        commands.Verify(c => c.SetSystemMode(SystemMode.Supervise), Times.Once);
    }

    [Fact]
    public async Task StartRecording_ConnectionLost_RecordsInterruptionReason()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        Mock<ILogger> logger = new();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        using RawDataRecorder sut = new(device.Object, eventManager, logger.Object, eventQueueSettleWindowMs: 0);

        sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100, waitUntilFinished: false);
        device.Setup(d => d.ConnectionStatus).Returns(ConnectionStatus.Disconnected);

        await WaitUntilAsync(() => !sut.IsRecording);

        logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("connection error", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void BlobSize_SensorOutOfRange_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.BlobSize(0));
    }

    [Fact]
    public void ClearBlob_ResetsBlobSizeAndRecordingStartTime()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);
        sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100, waitUntilFinished: false);

        sut.ClearBlob(1);

        Assert.Equal(0, sut.BlobSize(1));
        Assert.Null(sut.RecordingStartTimeUtc(1));
    }

    [Fact]
    public void BlobContents_CurrentlyRecordingSensor_Throws()
    {
        (Mock<IVSEDevice> device, DeviceEventManager eventManager, _, _) = CreateDevice();
        using RawDataRecorder sut = new(device.Object, eventManager, logger: null, eventQueueSettleWindowMs: 0);
        sut.StartRecording(sensor: 1, sampleRate: 50_000, duration: 100, waitUntilFinished: false);

        Assert.Throws<InvalidOperationException>(() => sut.BlobContents(1));
    }
}
