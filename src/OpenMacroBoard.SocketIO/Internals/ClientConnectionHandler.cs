using OpenMacroBoard.SDK;
using Overby.Extensions.AsyncBinaryReaderWriter;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenMacroBoard.SocketIO.Internals;

internal sealed class ClientConnectionHandler : IDisposable
{
    private readonly IMacroBoard board;
    private readonly TcpClient client;
    private readonly CancellationToken cancellationToken;
    private readonly ConcurrentQueue<(int, bool)> keyUpdates = new();
    private readonly AutoResetEvent keyUpdateEvent = new(false);

    public ClientConnectionHandler(
        IMacroBoard board,
        TcpClient client,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(client);

        this.board = board;
        this.client = client;
        this.cancellationToken = cancellationToken;

        this.board.KeyStateChanged += Board_KeyStateChanged;

        this.cancellationToken.Register(() =>
        {
            try
            {
                keyUpdateEvent.Set();
            }
            catch (ObjectDisposedException)
            {
                // we don't care about that exception.
            }
        });
    }

    public async Task HandleConnectionAsync()
    {
        try
        {
            var incommingTrafficTask = HandleIncommingTrafficAsync();
            var outgoingTraffic = HandleOutGoingTrafficAsync();

            await Task.WhenAll(incommingTrafficTask, outgoingTraffic);
        }
        catch (OperationCanceledException)
        {
            // Happens when we are shutting down
        }
        finally
        {
            board.KeyStateChanged -= Board_KeyStateChanged;
            client.Dispose();
            keyUpdateEvent.Dispose();
        }
    }

    public void SendKeyUpdate(int keyId, bool isDown)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        keyUpdates.Enqueue((keyId, isDown));
        keyUpdateEvent.Set();
    }

    public void Dispose()
    {
        keyUpdateEvent.Dispose();
    }

    private void Board_KeyStateChanged(object? sender, KeyEventArgs e)
    {
        SendKeyUpdate(e.Key, e.IsDown);
    }

    private async Task HandleOutGoingTrafficAsync()
    {
        var stream = client.GetStream();
        using var writer = new AsyncBinaryWriter(stream, Encoding.UTF8, true);

        while (!cancellationToken.IsCancellationRequested)
        {
            keyUpdateEvent.WaitOne();

            while (keyUpdates.TryDequeue(out var result))
            {
                await writer.WriteAsync((byte)PackageType.KeyStateChange);
                await writer.WriteAsync((ushort)result.Item1);
                await writer.WriteAsync(result.Item2);
            }
        }
    }

    private async Task HandleIncommingTrafficAsync()
    {
        var stream = client.GetStream();
        using var reader = new AsyncBinaryReader(stream, Encoding.UTF8, true);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var packageType = (PackageType)(await reader.ReadByteAsync(cancellationToken));

                if (packageType == PackageType.SetBrightness)
                {
                    var percent = Math.Min(await reader.ReadByteAsync(cancellationToken), (byte)100);
                    board.SetBrightness(percent);
                }
                else if (packageType == PackageType.ShowLogo)
                {
                    board.ShowLogo();
                }
                else if (packageType == PackageType.SetKeyImage)
                {
                    var keyId = (int)(await reader.ReadUInt16Async());
                    var width = (int)(await reader.ReadUInt16Async());
                    var height = (int)(await reader.ReadUInt16Async());
                    var length = await reader.ReadInt32Async();
                    var data = await reader.ReadBytesAsync(length);

                    var keyBitmap = KeyBitmap.Black;

                    if (data.Length > 0)
                    {
                        keyBitmap = KeyBitmap.Create.FromBgr24Array(width, height, data);
                    }

                    board.SetKeyBitmap(keyId, keyBitmap);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down
        }
    }
}
