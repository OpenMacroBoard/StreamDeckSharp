using AwesomeAssertions;
using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using Xunit;

namespace OpenMacroBoard.Tests;

public class DeviceContextTests
{
    [Fact]
    public void Connection_events_are_processed_as_expected()
    {
        var fakeListenerA = new FakeDeviceStateListener();
        var fakeDeviceA = new FakeDeviceReference();
        var fakeDeviceB = new FakeDeviceReference();

        using var ctx = DeviceContext.Create();
        ctx.AddListener(fakeListenerA);

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, true, true));

        ctx.KnownDevices.Should().HaveCount(1);
        ctx.KnownDevices[0].Connected.Should().BeTrue();

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, true, false));

        ctx.KnownDevices.Should().HaveCount(1);
        ctx.KnownDevices[0].Connected.Should().BeTrue();

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, false, false));

        ctx.KnownDevices.Should().HaveCount(1);
        ctx.KnownDevices[0].Connected.Should().BeFalse();

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, true, false));

        ctx.KnownDevices.Should().HaveCount(1);
        ctx.KnownDevices[0].Connected.Should().BeTrue();

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceB, false, false));

        ctx.KnownDevices.Should().HaveCount(2);
        ctx.KnownDevices[0].Connected.Should().BeTrue();
        ctx.KnownDevices[1].Connected.Should().BeFalse();

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceB, true, false));

        ctx.KnownDevices.Should().HaveCount(2);
        ctx.KnownDevices[0].Connected.Should().BeTrue();
        ctx.KnownDevices[1].Connected.Should().BeTrue();
    }

    [Fact]
    public void Only_report_real_status_changes()
    {
        var fakeListenerA = new FakeDeviceStateListener();
        var fakeDeviceA = new FakeDeviceReference();
        var reports = new List<DeviceStateReport>();

        void LogReport(DeviceStateReport report)
        {
            reports.Add(report);
        }

        using var ctx = DeviceContext.Create();
        ctx.AddListener(fakeListenerA);

        using var subscription = ctx.DeviceStateReports.Subscribe(LogReport);

        reports.Count.Should().Be(0);

        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, true, false));

        reports.Count.Should().Be(1);
        // even though the device listener didn't report the device as new
        // the context does, because the device is new to this context
        reports[^1].NewDevice.Should().BeTrue();
        reports[^1].Connected.Should().BeTrue();

        // device listener reports the same thing again
        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, true, false));

        // number of reports hasn't changed.
        reports.Count.Should().Be(1);

        // report disconnect
        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, false, false));

        reports.Count.Should().Be(2);
        reports[^1].NewDevice.Should().BeFalse();
        reports[^1].Connected.Should().BeFalse();

        // report disconnect again
        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, false, false));

        // no new report
        reports.Count.Should().Be(2);

        // device listener send connected state
        fakeListenerA.OnNext(new DeviceStateReport(fakeDeviceA, true, false));

        reports.Count.Should().Be(3);
    }

    [Fact]
    public void Disposable_listeners_are_disposed_with_the_context()
    {
        var listener = new FakeDisposableDeviceListener();

        using (var ctx = DeviceContext.Create())
        {
            ctx.AddListener(listener);
            listener.DisposeCalls.Should().Be(0);
            listener.SubscriptionCalls.Should().Be(1);
        }

        listener.DisposeCalls.Should().Be(1);
    }

    private sealed class FakeDisposableDeviceListener :
        IObservable<DeviceStateReport>,
        IDisposable
    {
        public int DisposeCalls { get; set; }
        public int SubscriptionCalls { get; set; }

        public void Dispose()
        {
            DisposeCalls++;
        }

        public IDisposable Subscribe(IObserver<DeviceStateReport> observer)
        {
            SubscriptionCalls++;
            return DoNothingDisposable.Instance;
        }
    }

    private sealed class FakeDeviceReference : IDeviceReference
    {
        public string DeviceName { get; set; }
        public IKeyLayout Keys { get; }

        public IMacroBoard Open()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeDeviceStateListener :
        IObservable<DeviceStateReport>,
        IObserver<DeviceStateReport>
    {
        private static readonly List<IObserver<DeviceStateReport>> subscribedObservers = [];


        public void OnCompleted()
        {
            foreach (var observer in subscribedObservers)
            {
                observer.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            foreach (var observer in subscribedObservers)
            {
                observer.OnError(error);
            }
        }

        public void OnNext(DeviceStateReport value)
        {
            foreach (var observer in subscribedObservers)
            {
                observer.OnNext(value);
            }
        }

        public IDisposable Subscribe(IObserver<DeviceStateReport> observer)
        {
            subscribedObservers.Add(observer);
            return DoNothingDisposable.Instance;
        }
    }

    private sealed class DoNothingDisposable : IDisposable
    {
        public static IDisposable Instance { get; } = new DoNothingDisposable();

        private DoNothingDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
