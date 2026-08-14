using System.Diagnostics;
using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;
using Moq;
using ViciOne.Communication.Vse;
using ViciOne.Communication.Vse.Data;
using ViciOne.Driver.Vse.Interfaces;

using static ViciOne.Driver.Vse.Tests.VseTestDevice;

namespace ViciOne.Driver.Vse.Tests;

public class VseDeviceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidAddress_Throws(string address)
    {
        using VseConnection connection = new(Mock.Of<IVSEDevice>(), logger: null);

        Assert.Throws<ArgumentException>(() => new VseDevice(address, connection, Mock.Of<ILogger>()));
    }

    [Fact]
    public void ConnectedEvent_TriggersRebuildAndMarksInitialized()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, _) = CreateDevice();
        using (device)
        {
            Assert.False(device.Initialized);
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            Assert.True(device.Initialized);
            Assert.True(device.GetIsServiceElementExisting(CounterAddress()));
        }
    }

    [Fact]
    public void ParametersChanged_NotRaisedOnFirstRebuild_RaisedOnSubsequentRebuild()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, _) = CreateDevice();
        using (device)
        {
            int raised = 0;
            device.ParametersChanged += () => raised++;

            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);
            Assert.Equal(0, raised);

            RaiseParametersChanged(sdk);
            WaitUntil(() => raised > 0);
            Assert.Equal(1, raised);
        }
    }

    [Fact]
    public void RegisterAddress_DispatchesPushedChangesToTheFunctionBlock()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, Action<ICounterDataSample> pushCounter) = CreateDevice();
        using (device)
        {
            Mock<IVseSubscriberFunctionBlock> functionBlock = new();
            device.RegisterAddress(CounterAddress(), functionBlock.Object);

            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            // Subscribing reports the current value once; this is about what a later push does.
            functionBlock.Invocations.Clear();

            Mock<ICounterDataSample> sample = new();
            sample.Setup(s => s.Id).Returns(0);
            sample.Setup(s => s.GetValue()).Returns(42f);
            pushCounter(sample.Object);

            functionBlock.Verify(f => f.SetValue(It.Is<Counter>(c => c.Id == 0)), Times.Once);
        }
    }

    [Fact]
    public void RegisterAddress_AfterDeviceAlreadyInitialized_SubscribesImmediately()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, Action<ICounterDataSample> pushCounter) = CreateDevice();
        using (device)
        {
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            Mock<IVseSubscriberFunctionBlock> functionBlock = new();
            device.RegisterAddress(CounterAddress(), functionBlock.Object);

            // Registering after the device is up subscribes right away, which reports the current value; this
            // is about the push that follows.
            functionBlock.Invocations.Clear();

            Mock<ICounterDataSample> sample = new();
            sample.Setup(s => s.Id).Returns(0);
            sample.Setup(s => s.GetValue()).Returns(1f);
            pushCounter(sample.Object);

            functionBlock.Verify(f => f.SetValue(It.IsAny<Counter>()), Times.Once);
        }
    }

    [Fact]
    public void RegisterAddress_SameAddressTwice_BothFunctionBlocksAreDispatchedTo()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, Action<ICounterDataSample> pushCounter) = CreateDevice();
        using (device)
        {
            Mock<IVseSubscriberFunctionBlock> first = new();
            Mock<IVseSubscriberFunctionBlock> second = new();
            device.RegisterAddress(CounterAddress(), first.Object);
            device.RegisterAddress(CounterAddress(), second.Object);

            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            // Both were reported the current value on subscribe; this is about the push reaching both.
            first.Invocations.Clear();
            second.Invocations.Clear();

            Mock<ICounterDataSample> sample = new();
            sample.Setup(s => s.Id).Returns(0);
            sample.Setup(s => s.GetValue()).Returns(1f);
            pushCounter(sample.Object);

            first.Verify(f => f.SetValue(It.IsAny<Counter>()), Times.Once);
            second.Verify(f => f.SetValue(It.IsAny<Counter>()), Times.Once);
        }
    }

    [Fact]
    public void RegisterAddress_ReportsTheCurrentValueWithoutWaitingForAChange()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, _) = CreateDevice();
        using (device)
        {
            Mock<IVseSubscriberFunctionBlock> functionBlock = new();
            device.RegisterAddress(CounterAddress(), functionBlock.Object);

            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            // The SDK reports a value only when it changes. A value that sits at its default and stays there -
            // an alarm that never fires, a counter that never counts - would otherwise never be reported at
            // all, leaving subscribers unable to tell "nothing happened" from "never heard from".
            functionBlock.Verify(f => f.SetValue(It.IsAny<Counter>()), Times.Once);
        }
    }

    [Fact]
    public void UnregisterAddress_StopsDispatchingToThatFunctionBlock()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, Action<ICounterDataSample> pushCounter) = CreateDevice();
        using (device)
        {
            Mock<IVseSubscriberFunctionBlock> functionBlock = new();
            device.RegisterAddress(CounterAddress(), functionBlock.Object);
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            device.UnregisterAddress(functionBlock.Object);

            // Subscribing reported the current value; nothing more may arrive after unregistering.
            functionBlock.Invocations.Clear();

            Mock<ICounterDataSample> sample = new();
            sample.Setup(s => s.Id).Returns(0);
            sample.Setup(s => s.GetValue()).Returns(1f);
            pushCounter(sample.Object);

            functionBlock.Verify(f => f.SetValue(It.IsAny<Counter>()), Times.Never);
        }
    }

    [Fact]
    public void PushLatency_ArrivesSynchronouslyRatherThanOnTheOldOneSecondPollInterval()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, Action<ICounterDataSample> pushCounter) = CreateDevice();
        using (device)
        {
            Mock<IVseSubscriberFunctionBlock> functionBlock = new();
            device.RegisterAddress(CounterAddress(), functionBlock.Object);
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            // Subscribing reported the current value; the measurement below is about the push alone.
            functionBlock.Invocations.Clear();

            Mock<ICounterDataSample> sample = new();
            sample.Setup(s => s.Id).Returns(0);
            sample.Setup(s => s.GetValue()).Returns(1f);

            // The old ifmIoTCore-tree adapter re-discovered changes by polling every 1000ms; the VSE-SDK's
            // monitoring channel instead pushes a sample straight into VseDevice's dispatch. This proves that
            // dispatch happens synchronously with the SDK callback - nothing queues or delays it in between -
            // rather than merely trusting that removing the poll loop implies low latency.
            var stopwatch = Stopwatch.StartNew();
            pushCounter(sample.Object);
            stopwatch.Stop();

            functionBlock.Verify(f => f.SetValue(It.IsAny<Counter>()), Times.Once);
            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"Push dispatch took {stopwatch.ElapsedMilliseconds} ms - expected near-instant, well under the old 1000 ms poll interval.");
        }
    }

    [Fact]
    public void GetIsServiceElementExisting_UnknownAddress_ReturnsFalse()
    {
        (VseDevice device, Mock<IVSEDevice> sdk, _, _) = CreateDevice();
        using (device)
        {
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            Assert.False(device.GetIsServiceElementExisting("does-not-exist"));
        }
    }

    [Fact]
    public void Write_ToVariantsAddress_WhileSupervisingAndConnected_EventuallyCallsSetVariant()
    {
        Mock<IVariant> variant = new();
        variant.Setup(v => v.Id).Returns(0);
        variant.Setup(v => v.ActivatedObjects).Returns([]);

        (VseDevice device, Mock<IVSEDevice> sdk, Mock<ICommands> commands, _) = CreateDevice(variants: [variant.Object]);
        using (device)
        {
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            device.Write(ActiveVariantAddress(), 1);

            WaitUntil(() => commands.Invocations.Count > 0, timeoutMs: 5000);
            commands.Verify(c => c.SetVariant(1), Times.Once);
        }
    }

    [Fact]
    public void Write_WhileNotSupervising_NeverCallsSetVariant()
    {
        Mock<IVariant> variant = new();
        variant.Setup(v => v.Id).Returns(0);
        variant.Setup(v => v.ActivatedObjects).Returns([]);

        (VseDevice device, Mock<IVSEDevice> sdk, Mock<ICommands> commands, _) = CreateDevice(systemMode: SystemMode.Measure, variants: [variant.Object]);
        using (device)
        {
            RaiseConnected(sdk);
            WaitUntil(() => device.Initialized);

            device.Write(ActiveVariantAddress(), 1);

            Thread.Sleep(1200);
            commands.Verify(c => c.SetVariant(It.IsAny<int>()), Times.Never);
        }
    }
}
