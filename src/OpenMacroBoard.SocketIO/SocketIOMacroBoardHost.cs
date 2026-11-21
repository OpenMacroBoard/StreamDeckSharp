using BeaconLib;
using Newtonsoft.Json;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO.Internals;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenMacroBoard.SocketIO;

/// <summary>
/// A SocketIO device host. This host is needed to implement your own SocketIO device.
/// </summary>
public sealed class SocketIOMacroBoardHost : IDisposable
{
    private readonly Beacon beacon;
    private readonly TcpListener listener;
    private readonly CancellationTokenSource shutdownTokenSource;
    private readonly CancellationToken shutdownToken;

    private readonly ManualResetEventSlim handleIncomingConnectionsEnded = new(false);

    private readonly Lock listLock = new();
    private readonly List<Task> clientConnections = [];
    private readonly IMacroBoard macroBoard;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketIOMacroBoardHost"/> class.
    /// </summary>
    /// <param name="macroBoard">The board which will be controlled by this host.</param>
    /// <param name="deviceName">The device name which will be reported to the SocketIO listeners.</param>
    /// <param name="bindIpAddress">The local IP address this host is listening on.</param>
    /// <param name="port">The port this host is listening on. Defaults to zero which means the OS will decide the port.</param>
    public SocketIOMacroBoardHost(
        IMacroBoard macroBoard,
        string deviceName,
        IPAddress bindIpAddress,
        ushort port = 0
    )
    {
        ArgumentNullException.ThrowIfNull(deviceName);
        ArgumentNullException.ThrowIfNull(macroBoard);

        this.macroBoard = macroBoard;

        shutdownTokenSource = new CancellationTokenSource();
        shutdownToken = shutdownTokenSource.Token;

        listener = new TcpListener(bindIpAddress, port);
        listener.Start();

        HostEndpoint = (IPEndPoint)listener.LocalEndpoint;

        _ = Task.Run(HandleIncomingConnectionsAsync);

        var meta = new SocketIODeviceMetaData()
        {
            DeviceName = deviceName,
            MetaVersion = 1,
            Keys = new KeyDetails()
            {
                CountX = macroBoard.Keys.CountX,
                CountY = macroBoard.Keys.CountY,

                // even though in theory key width and height and the x and y distance
                // could be different, we don't really care and just use the x direction
                KeySize = macroBoard.Keys.KeySize,
                GapSize = macroBoard.Keys.GapSize,
            },
        };

        // Start broadcasting information about this server
        beacon = new Beacon(SocketIOConstants.BeaconIdentifier, (ushort)HostEndpoint.Port, bindIpAddress)
        {
            BeaconData = JsonConvert.SerializeObject(meta),
        };

        beacon.Start();
    }

    /// <summary>
    /// The listening endpoint for this SocketIO host.
    /// </summary>
    public IPEndPoint HostEndpoint { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Stop broadcasting
        beacon.Stop();
        beacon.Dispose();

        // initiate shutdown
        shutdownTokenSource.Cancel();
        listener.Stop();

        // wait for connection handler to complete
        handleIncomingConnectionsEnded.Wait();

        // wait for open connections.
        // we don't have to lock the list because AddToClientHandlerTasks can't happen
        // because it already shut down at that point.

        var waitAllBlock = new ManualResetEventSlim(false);

        // Set reset event when all client connections are closed.
#pragma warning disable AV2235 // Call to Task.ContinueWith should be replaced with an await expression
        _ = Task.WhenAll(clientConnections).ContinueWith(_ => waitAllBlock.Set());
#pragma warning restore AV2235

        waitAllBlock.Wait();
        shutdownTokenSource.Dispose();
    }

    private async Task HandleIncomingConnectionsAsync()
    {
        try
        {
            while (!shutdownToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                var clientHandleTask = Task.Run(async () => await new ClientConnectionHandler(macroBoard, client, shutdownToken).HandleConnectionAsync());
                AddToClientHandlerTasks(clientHandleTask);
            }
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
        {
            // just ignore those, just means a shutdown was requested.
        }
        finally
        {
            handleIncomingConnectionsEnded.Set();
        }
    }

    private void AddToClientHandlerTasks(Task clientHandlerTask)
    {
        lock (listLock)
        {
            for (int i = 0; i < clientConnections.Count; i++)
            {
                // find a slot with a completed task and swap that out.
                // we do that to prevent the list from growing infinitely large

                if (clientConnections[i].IsCompleted)
                {
                    clientConnections[i] = clientHandlerTask;
                    return;
                }
            }

            clientConnections.Add(clientHandlerTask);
        }
    }
}
