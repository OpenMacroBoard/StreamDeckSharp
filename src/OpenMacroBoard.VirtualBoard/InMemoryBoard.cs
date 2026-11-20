using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// A macro board that only exists in memory. Applications draw on it through SocketIO,
/// viewers (browser pages) show it and press its keys.
/// </summary>
/// <remarks>
/// <para>
/// A key is down while at least one viewer holds it. All members are thread-safe. The events
/// are raised while an internal lock is held, so handlers must return quickly.
/// </para>
/// </remarks>
internal sealed class InMemoryBoard : IMacroBoard
{
    private readonly Lock gate = new();
    private readonly KeyBitmap[] keyImages;
    private readonly HashSet<long>[] keyHolders;
    private byte brightness = 100;

    public InMemoryBoard(GridKeyLayout keyLayout)
    {
        ArgumentNullException.ThrowIfNull(keyLayout);

        Keys = keyLayout;
        keyImages = new KeyBitmap[keyLayout.Count];
        keyHolders = new HashSet<long>[keyLayout.Count];

        for (var i = 0; i < keyLayout.Count; i++)
        {
            keyImages[i] = KeyBitmap.Black;
            keyHolders[i] = [];
        }
    }

    /// <inheritdoc/>
    public event EventHandler<KeyEventArgs>? KeyStateChanged;

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs>? ConnectionStateChanged
    {
        add
        {
            // The board in memory is always connected, so this event is never raised.
        }

        remove
        {
            // See add.
        }
    }

    /// <summary>
    /// Raised with the ID of a key after its image changed.
    /// </summary>
    public event Action<int>? KeyImageChanged;

    /// <summary>
    /// Raised with the new value after the brightness changed.
    /// </summary>
    public event Action<byte>? BrightnessChanged;

    /// <inheritdoc/>
    public IKeyLayout Keys { get; }

    /// <inheritdoc/>
    public bool IsConnected => true;

    /// <summary>
    /// The brightness in percent (0 to 100).
    /// </summary>
    public byte Brightness
    {
        get
        {
            lock (gate)
            {
                return brightness;
            }
        }
    }

    /// <summary>
    /// Whether anyone listens for key events, for example a connected application.
    /// </summary>
    public bool HasKeyStateSubscribers => KeyStateChanged is not null;

    /// <inheritdoc/>
    public void SetBrightness(byte percent)
    {
        lock (gate)
        {
            brightness = Math.Min(percent, (byte)100);
            BrightnessChanged?.Invoke(brightness);
        }
    }

    /// <inheritdoc/>
    public void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keyId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(keyId, keyImages.Length);

        lock (gate)
        {
            keyImages[keyId] = bitmapData ?? KeyBitmap.Black;
            KeyImageChanged?.Invoke(keyId);
        }
    }

    /// <inheritdoc/>
    public void ShowLogo()
    {
        // The board itself has no logo. DecoratedVirtualBoard adds one.
    }

    /// <inheritdoc/>
    public string GetFirmwareVersion()
    {
        return string.Empty;
    }

    /// <inheritdoc/>
    public string GetSerialNumber()
    {
        return string.Empty;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose
    }

    public KeyBitmap GetKeyImage(int keyId)
    {
        lock (gate)
        {
            return keyImages[keyId];
        }
    }

    public bool IsKeyDown(int keyId)
    {
        lock (gate)
        {
            return keyHolders[keyId].Count > 0;
        }
    }

    /// <summary>
    /// Records that a viewer holds or releases a key.
    /// </summary>
    public void SetViewerKeyState(long viewerId, int keyId, bool isDown)
    {
        if (keyId < 0 || keyId >= keyHolders.Length)
        {
            return;
        }

        lock (gate)
        {
            var holders = keyHolders[keyId];
            var wasDown = holders.Count > 0;

            if (isDown)
            {
                holders.Add(viewerId);
            }
            else
            {
                holders.Remove(viewerId);
            }

            var nowDown = holders.Count > 0;

            if (nowDown != wasDown)
            {
                KeyStateChanged?.Invoke(this, new KeyEventArgs(keyId, nowDown));
            }
        }
    }

    /// <summary>
    /// Releases every key a viewer holds, for example when its connection ends.
    /// </summary>
    public void ReleaseViewer(long viewerId)
    {
        for (var keyId = 0; keyId < keyHolders.Length; keyId++)
        {
            SetViewerKeyState(viewerId, keyId, isDown: false);
        }
    }
}
