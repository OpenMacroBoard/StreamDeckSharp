using OpenMacroBoard.SDK;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace OpenMacroBoard.SocketIO.Internals;

internal sealed class SocketIOMacroBoardClient : IMacroBoard
{
    private readonly IPEndPoint ipEndPoint;

    private readonly CancellationTokenSource shutdownSource;
    private readonly CancellationToken shutdownToken;

    private readonly ManualResetEventSlim endReadEventLoop = new(false);
    private readonly ManualResetEventSlim endWriteEventLoop = new(false);
    private readonly SemaphoreSlim ensureConnectionLock = new(1, 1);

    private readonly AutoResetEvent writeTrigger = new(false);

    private readonly Lock brightnessSync = new();
    private readonly Lock keyBitmapSync = new();

    private readonly bool[] keyBitmapSendRequested;
    private readonly KeyBitmap[] keyBitmaps;

    private TcpClientBinaryIO? tcpClient = null;

    private bool brightnessSendRequested = false;
    private byte desiredBrightness = 250;
    private bool showLogoRequested = false;

    public SocketIOMacroBoardClient(
        IPEndPoint ipEndPoint,
        GridKeyLayout keys
    )
    {
        ArgumentNullException.ThrowIfNull(ipEndPoint);
        ArgumentNullException.ThrowIfNull(keys);

        this.ipEndPoint = ipEndPoint;
        Keys = keys;

        keyBitmapSendRequested = new bool[keys.Count];
        keyBitmaps = new KeyBitmap[keys.Count];

        shutdownSource = new CancellationTokenSource();
        shutdownToken = shutdownSource.Token;

        _ = Task.Run(WriteLoopAsync);
        _ = Task.Run(ReadLoopAsync);
    }

    public event EventHandler<KeyEventArgs>? KeyStateChanged;
    public event EventHandler<ConnectionEventArgs>? ConnectionStateChanged;

    public IKeyLayout Keys { get; }
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public void SetBrightness(byte percent)
    {
        if (percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        lock (brightnessSync)
        {
            desiredBrightness = percent;
            brightnessSendRequested = true;
        }

        writeTrigger.Set();
    }

    public void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        lock (keyBitmapSync)
        {
            keyBitmaps[keyId] = bitmapData;
            keyBitmapSendRequested[keyId] = true;
        }

        writeTrigger.Set();
    }

    public void ShowLogo()
    {
        showLogoRequested = true;
        writeTrigger.Set();
    }

    public string GetFirmwareVersion()
    {
        return string.Empty;
    }

    public string GetSerialNumber()
    {
        return string.Empty;
    }

    public void Dispose()
    {
        shutdownSource.Cancel();

        ShowLogo();
        writeTrigger.Set();
        endWriteEventLoop.Wait();

        tcpClient?.Dispose();

        endReadEventLoop.Wait();

        endWriteEventLoop.Dispose();
        endReadEventLoop.Dispose();
        shutdownSource.Dispose();
    }

    private void SetConnectionStateAndRaiseEvent(bool connectionState)
    {
        if (IsConnected == connectionState)
        {
            return;
        }

        IsConnected = connectionState;
        ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(connectionState));
    }

    private async Task RenewTcpClientAsync()
    {
        shutdownToken.ThrowIfCancellationRequested();

        await ensureConnectionLock.WaitAsync();

        try
        {
            while (true)
            {
                shutdownToken.ThrowIfCancellationRequested();

                // if TCP client is good, do nothing.
                if (tcpClient?.Client?.Connected == true)
                {
                    SetConnectionStateAndRaiseEvent(true);
                    return;
                }

                SetConnectionStateAndRaiseEvent(false);

                try
                {
                    // (Re-)connect

                    tcpClient?.Dispose();
                    tcpClient = null;
                    tcpClient = await TcpClientBinaryIO.ConnectAsync(ipEndPoint);

                    SetConnectionStateAndRaiseEvent(tcpClient?.Client?.Connected == true);
                    return;
                }
                catch
                {
                    // we don't want to "die" because of exceptions here.

                    // wait a little bit
                    await Task.Delay(500);
                }
            }
        }
        finally
        {
            ensureConnectionLock.Release();
        }
    }

    [SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "False positive.")]
    private async Task WriteLoopAsync()
    {
        const int startTimeout = 250;
        const int maxTimout = 10000;

        try
        {
            await RenewTcpClientAsync();
            bool successfulRun = true;

            int timeout = startTimeout;

            void ResetTimeout()
            {
                timeout = startTimeout;
            }

            void IncreaseTimeout()
            {
                timeout *= 2;

                if (timeout > maxTimout)
                {
                    timeout = maxTimout;
                }
            }

            while (true)
            {
                try
                {
                    if (tcpClient is null)
                    {
                        throw new InvalidOperationException("Unexpected state: TcpClient net set up correctly.");
                    }

                    if (successfulRun)
                    {
                        if (shutdownToken.IsCancellationRequested)
                        {
                            return;
                        }

                        // if the last run was successful, wait for another trigger.
                        // if it wasn't don't wait and try to send again.
                        writeTrigger.WaitOne();

                        // Do not exit here. Make sure we add another run before
                        // we dispose (to display the logo, etc.).
                    }

                    successfulRun = false;

                    if (showLogoRequested)
                    {
                        await tcpClient.Writer.WriteAsync((byte)PackageType.ShowLogo);
                        showLogoRequested = false;
                    }

                    // copy the value because it could be set again in another thread.
                    if (brightnessSendRequested)
                    {
                        var sendingBrightness = desiredBrightness;

                        await tcpClient.Writer.WriteAsync((byte)PackageType.SetBrightness);
                        await tcpClient.Writer.WriteAsync(sendingBrightness);

                        lock (brightnessSync)
                        {
                            if (desiredBrightness == sendingBrightness)
                            {
                                brightnessSendRequested = false;
                            }
                        }
                    }

                    // update keys
                    for (int i = 0; i < keyBitmapSendRequested.Length; i++)
                    {
                        if (keyBitmapSendRequested[i])
                        {
                            var sendingBitmap = keyBitmaps[i];

                            // FIX: get rid of array copy.
                            var data = ((IKeyBitmapDataAccess)sendingBitmap).GetData().ToArray();

                            await tcpClient.Writer.WriteAsync((byte)PackageType.SetKeyImage);
                            await tcpClient.Writer.WriteAsync((ushort)i);
                            await tcpClient.Writer.WriteAsync((ushort)sendingBitmap.Width);
                            await tcpClient.Writer.WriteAsync((ushort)sendingBitmap.Height);
                            await tcpClient.Writer.WriteAsync(data.Length);
                            await tcpClient.Writer.WriteAsync(data);

                            lock (keyBitmapSync)
                            {
                                if (keyBitmaps[i] == sendingBitmap)
                                {
                                    keyBitmapSendRequested[i] = false;
                                }
                            }
                        }
                    }

                    successfulRun = true;
                    ResetTimeout();
                }
                catch
                {
                    // catch all, we don't want the write loop to die because of exceptions.

                    if (shutdownToken.IsCancellationRequested)
                    {
                        // if we are shutting down, do not try to reconnect.
                        return;
                    }

                    // something went wrong so we report a disconnect state.
                    IsConnected = false;
                    ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(false));

                    // wait to cool down
                    await Task.Delay(timeout);

                    // make sure the client is healthy (try to reconnect if needed)
                    await RenewTcpClientAsync();

                    // if there is a problem with the connection we
                    // don't want to spam and back off a little.
                    IncreaseTimeout();
                }
            }
        }
        finally
        {
            endWriteEventLoop.Set();
        }
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!shutdownToken.IsCancellationRequested)
            {
                try
                {
                    await RenewTcpClientAsync();

                    while (!shutdownToken.IsCancellationRequested)
                    {
                        var packageType = (PackageType)await tcpClient!.Reader.ReadByteAsync(shutdownToken);

                        if (packageType == PackageType.KeyStateChange)
                        {
                            var keyId = (int)await tcpClient.Reader.ReadUInt16Async(shutdownToken);
                            var isDown = await tcpClient.Reader.ReadBooleanAsync(shutdownToken);

                            KeyStateChanged?.Invoke(this, new KeyEventArgs(keyId, isDown));
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    // other end dropped connection
                    // force creating a new client

#pragma warning disable S3966 // Objects should not be disposed more than once
                    tcpClient?.Dispose();
#pragma warning restore S3966
                }
            }
        }
        finally
        {
            endReadEventLoop.Set();
        }
    }
}
