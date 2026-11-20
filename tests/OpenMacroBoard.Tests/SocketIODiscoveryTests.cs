using AwesomeAssertions;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Checks that applications find boards with the default settings, where both sides
/// are bound to the loopback address.
/// </summary>
public class SocketIODiscoveryTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public void ListenerFindsAllBoardsOnTheLoopbackAddress()
    {
        // Unique names, because other tests host boards on this machine at the same time.
        var firstName = "Discovery " + Guid.NewGuid();
        var secondName = "Discovery " + Guid.NewGuid();
        var layout = new GridKeyLayout(3, 2, 16, 4);

        using var firstBoard = new FakeMacroBoard(layout);
        using var secondBoard = new FakeMacroBoard(layout);
        using var firstHost = new SocketIOMacroBoardHost(firstBoard, firstName, IPAddress.Loopback);
        using var secondHost = new SocketIOMacroBoardHost(secondBoard, secondName, IPAddress.Loopback);

        var found = new ConcurrentDictionary<string, IPEndPoint>();
        using var listener = new SocketIOBoardListener();

        using var subscription = listener.Subscribe(new Observer(report =>
        {
            if (report.Connected && report.DeviceReference is SocketIODeviceReference reference)
            {
                found[reference.DeviceName] = reference.IPEndPoint;
            }
        }));

        SpinWait.SpinUntil(() => found.ContainsKey(firstName) && found.ContainsKey(secondName), Timeout);

        found.Keys.Where(name => name == firstName || name == secondName)
            .Should().BeEquivalentTo([firstName, secondName]);

        found[firstName].Should().Be(new IPEndPoint(IPAddress.Loopback, firstHost.HostEndpoint.Port));
        found[secondName].Should().Be(new IPEndPoint(IPAddress.Loopback, secondHost.HostEndpoint.Port));
    }

    private sealed class Observer(Action<DeviceStateReport> onNext) : IObserver<DeviceStateReport>
    {
        public void OnCompleted()
        {
            // Nothing to do.
        }

        public void OnError(Exception error)
        {
            // Nothing to do.
        }

        public void OnNext(DeviceStateReport value)
        {
            onNext(value);
        }
    }
}
