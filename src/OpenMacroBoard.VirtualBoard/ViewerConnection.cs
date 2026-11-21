using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// One viewer (a browser page) that is connected through a WebSocket.
/// </summary>
/// <remarks>
/// <para>
/// Key images are not queued one by one. A changed key is only marked, and the image it has
/// at the time of sending is sent. A slow viewer therefore skips images, it never falls behind.
/// </para>
/// </remarks>
internal sealed class ViewerConnection : IDisposable
{
    private const int MaxIncomingMessageSize = 1024;

    private static long lastViewerId;

    private readonly long viewerId = Interlocked.Increment(ref lastViewerId);
    private readonly Lock gate = new();
    private readonly SemaphoreSlim pendingSignal = new(0);
    private readonly List<byte[]> pendingTextMessages = [];
    private readonly SortedSet<int> changedKeys = [];

    private readonly WebSocket socket;
    private readonly InMemoryBoard board;

    public ViewerConnection(WebSocket socket, InMemoryBoard board)
    {
        this.socket = socket;
        this.board = board;
    }

    /// <summary>
    /// Serves the viewer until it disconnects or <paramref name="stopping"/> is cancelled.
    /// </summary>
    public async Task RunAsync(byte[] boardMessage, CancellationToken stopping)
    {
        using var connectionEnd = CancellationTokenSource.CreateLinkedTokenSource(stopping);

        board.KeyImageChanged += MarkKeyChanged;
        board.KeyStateChanged += QueueKeyState;
        board.BrightnessChanged += QueueBrightness;

        try
        {
            // The description of the board comes first, then the current state. Changes that
            // happen meanwhile are already recorded by the handlers above.
            QueueText(boardMessage);
            QueueText(ViewerProtocol.BrightnessMessage(board.Brightness));

            for (var keyId = 0; keyId < board.Keys.Count; keyId++)
            {
                MarkKeyChanged(keyId);

                if (board.IsKeyDown(keyId))
                {
                    QueueText(ViewerProtocol.KeyStateMessage(keyId, isDown: true));
                }
            }

            var sending = SendLoopAsync(connectionEnd.Token);
            var receiving = ReceiveLoopAsync(connectionEnd.Token);

            await Task.WhenAny(sending, receiving);
            await connectionEnd.CancelAsync();

            await IgnoreConnectionEndAsync(sending);
            await IgnoreConnectionEndAsync(receiving);
        }
        finally
        {
            board.KeyImageChanged -= MarkKeyChanged;
            board.KeyStateChanged -= QueueKeyState;
            board.BrightnessChanged -= QueueBrightness;

            // A key this viewer still holds must not stay down forever.
            board.ReleaseViewer(viewerId);
        }
    }

    public void Dispose()
    {
        pendingSignal.Dispose();
    }

    private static async Task IgnoreConnectionEndAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // The connection ended.
        }
        catch (WebSocketException)
        {
            // The viewer went away without closing the connection properly.
        }
    }

    private void QueueKeyState(object? sender, KeyEventArgs e)
    {
        QueueText(ViewerProtocol.KeyStateMessage(e.Key, e.IsDown));
    }

    private void QueueBrightness(byte percent)
    {
        QueueText(ViewerProtocol.BrightnessMessage(percent));
    }

    private void QueueText(byte[] message)
    {
        lock (gate)
        {
            pendingTextMessages.Add(message);
        }

        pendingSignal.Release();
    }

    private void MarkKeyChanged(int keyId)
    {
        bool isNew;

        lock (gate)
        {
            isNew = changedKeys.Add(keyId);
        }

        if (isNew)
        {
            pendingSignal.Release();
        }
    }

    private async Task SendLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await pendingSignal.WaitAsync(cancellationToken);

            byte[][] textMessages;
            int[] keys;

            lock (gate)
            {
                textMessages = [.. pendingTextMessages];
                keys = [.. changedKeys];

                pendingTextMessages.Clear();
                changedKeys.Clear();
            }

            foreach (var message in textMessages)
            {
                await socket.SendAsync(message, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }

            foreach (var keyId in keys)
            {
                var message = ViewerProtocol.KeyImageMessage(keyId, board.GetKeyImage(keyId));
                await socket.SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxIncomingMessageSize];
        var length = 0;
        var isTooLong = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer.AsMemory(length), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            length += result.Count;

            if (!result.EndOfMessage)
            {
                if (length == buffer.Length)
                {
                    // Longer than any valid message. Drop the rest of it.
                    isTooLong = true;
                    length = 0;
                }

                continue;
            }

            if (!isTooLong
                && result.MessageType == WebSocketMessageType.Text
                && ViewerProtocol.TryReadKeyMessage(buffer.AsSpan(0, length), out var keyId, out var isDown))
            {
                board.SetViewerKeyState(viewerId, keyId, isDown);
            }

            length = 0;
            isTooLong = false;
        }
    }
}
