using System;
using System.Threading;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A macro board that only exists in memory. It remembers what an application does with it
/// (key images, brightness) and lets code press its keys.
/// </summary>
/// <remarks>
/// <para>
/// Use it to test an application without a device: pass it wherever an <see cref="IMacroBoard"/>
/// is expected, press keys with <see cref="SetKeyState"/> and check the result with
/// <see cref="GetKeyBitmap"/>, <see cref="Brightness"/> and <see cref="IsLogoShown"/>.
/// It can also be the model behind a board that is drawn on screen.
/// </para>
/// <para>
/// Like a device, it starts with its logo shown, and it ignores what is sent to it while it is
/// not connected (see <see cref="IsConnected"/>).
/// </para>
/// <para>
/// All members are thread-safe. The events are raised while an internal lock is held, so
/// handlers see the changes in the order they happened, and they must return quickly.
/// </para>
/// </remarks>
public sealed class InMemoryMacroBoard : IMacroBoard
{
    private readonly Lock gate = new();
    private readonly KeyBitmap[] keyBitmaps;
    private readonly bool[] keyStates;

    private bool isConnected = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryMacroBoard"/> class.
    /// </summary>
    /// <param name="keys">The keys of the board, for example a <see cref="GridKeyLayout"/>.</param>
    public InMemoryMacroBoard(IKeyLayout keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        Keys = keys;
        keyBitmaps = new KeyBitmap[keys.Count];
        keyStates = new bool[keys.Count];

        Array.Fill(keyBitmaps, KeyBitmap.Black);
    }

    /// <inheritdoc/>
    public event EventHandler<KeyEventArgs>? KeyStateChanged;

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Is raised after the image of a key was set, also if the same image was set again.
    /// </summary>
    public event EventHandler<KeyBitmapChangedEventArgs>? KeyBitmapChanged;

    /// <summary>
    /// Is raised after the brightness was set, also if the same value was set again.
    /// </summary>
    public event EventHandler? BrightnessChanged;

    /// <inheritdoc/>
    public IKeyLayout Keys { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the board is connected. Set it to simulate
    /// that the device is unplugged or plugged in again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While the board is not connected, images, brightness and key presses are ignored.
    /// Disconnecting releases all keys without raising <see cref="KeyStateChanged"/>, and
    /// connecting again shows the logo, like a device that was plugged in.
    /// </para>
    /// </remarks>
    public bool IsConnected
    {
        get
        {
            lock (gate)
            {
                return isConnected;
            }
        }

        set
        {
            lock (gate)
            {
                if (value == isConnected)
                {
                    return;
                }

                isConnected = value;

                if (value)
                {
                    ShowLogoCore();
                }
                else
                {
                    Array.Clear(keyStates);
                }

                ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(value));
            }
        }
    }

    /// <summary>
    /// Gets the brightness in percent (0 to 100) that was set last. It starts at 100.
    /// </summary>
    public byte Brightness
    {
        get
        {
            lock (gate)
            {
                return field;
            }
        }

        private set;
    }

    = 100;

    /// <summary>
    /// Gets a value indicating whether the board shows its logo: at the start and after
    /// <see cref="ShowLogo"/>, until the first key image is set.
    /// </summary>
    public bool IsLogoShown
    {
        get
        {
            lock (gate)
            {
                return field;
            }
        }

        private set;
    }

    = true;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> was called.
    /// </summary>
    public bool IsDisposed
    {
        get
        {
            lock (gate)
            {
                return field;
            }
        }

        private set;
    }

    /// <summary>
    /// Gets the firmware version <see cref="GetFirmwareVersion"/> returns. It is empty by default.
    /// </summary>
    public string FirmwareVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the serial number <see cref="GetSerialNumber"/> returns. It is empty by default.
    /// </summary>
    public string SerialNumber { get; init; } = string.Empty;

    /// <summary>
    /// Sets the brightness. Values above 100 count as 100.
    /// </summary>
    /// <param name="percent">Brightness in percent (0 - 100)</param>
    public void SetBrightness(byte percent)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!isConnected)
            {
                return;
            }

            Brightness = Math.Min(percent, (byte)100);
            BrightnessChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        ThrowIfNoKey(keyId);

        lock (gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!isConnected)
            {
                return;
            }

            IsLogoShown = false;
            keyBitmaps[keyId] = bitmapData ?? KeyBitmap.Black;
            KeyBitmapChanged?.Invoke(this, new KeyBitmapChangedEventArgs(keyId));
        }
    }

    /// <summary>
    /// Shows the logo. This board has no logo image: all keys become black and
    /// <see cref="IsLogoShown"/> is <see langword="true"/> until the next key image is set.
    /// </summary>
    public void ShowLogo()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!isConnected)
            {
                return;
            }

            ShowLogoCore();
        }
    }

    /// <inheritdoc/>
    public string GetFirmwareVersion()
    {
        return FirmwareVersion;
    }

    /// <inheritdoc/>
    public string GetSerialNumber()
    {
        return SerialNumber;
    }

    /// <summary>
    /// Gets the image a key shows: the one that was set last, in the size it was set with.
    /// A key that never got an image, or that is covered by the logo, is black.
    /// </summary>
    /// <param name="keyId">The index of the key.</param>
    /// <returns>The image of the key.</returns>
    public KeyBitmap GetKeyBitmap(int keyId)
    {
        ThrowIfNoKey(keyId);

        lock (gate)
        {
            return keyBitmaps[keyId];
        }
    }

    /// <summary>
    /// Gets a value indicating whether a key is held down at the moment.
    /// </summary>
    /// <param name="keyId">The index of the key.</param>
    /// <returns><see langword="true"/> if the key is down.</returns>
    public bool IsKeyDown(int keyId)
    {
        ThrowIfNoKey(keyId);

        lock (gate)
        {
            return keyStates[keyId];
        }
    }

    /// <summary>
    /// Presses or releases a key, like a finger on the device. Raises
    /// <see cref="KeyStateChanged"/> if that changes the state of the key.
    /// </summary>
    /// <param name="keyId">The index of the key.</param>
    /// <param name="isDown"><see langword="true"/> to press the key, <see langword="false"/> to release it.</param>
    public void SetKeyState(int keyId, bool isDown)
    {
        ThrowIfNoKey(keyId);

        lock (gate)
        {
            if (IsDisposed || !isConnected || keyStates[keyId] == isDown)
            {
                return;
            }

            keyStates[keyId] = isDown;
            KeyStateChanged?.Invoke(this, new KeyEventArgs(keyId, isDown));
        }
    }

    /// <summary>
    /// Marks the board as disposed (see <see cref="IsDisposed"/>). Calling it again does nothing.
    /// Afterwards the methods that send something to the board throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    public void Dispose()
    {
        lock (gate)
        {
            IsDisposed = true;
        }
    }

    private void ShowLogoCore()
    {
        IsLogoShown = true;

        for (var keyId = 0; keyId < keyBitmaps.Length; keyId++)
        {
            keyBitmaps[keyId] = KeyBitmap.Black;
            KeyBitmapChanged?.Invoke(this, new KeyBitmapChangedEventArgs(keyId));
        }
    }

    private void ThrowIfNoKey(int keyId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keyId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(keyId, keyBitmaps.Length);
    }
}
