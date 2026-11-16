using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Wraps an <see cref="IMacroBoard"/> and allows for hooks to implement "middle-ware" features.
/// </summary>
public abstract class MacroBoardAdapter : IMacroBoard
{
    private readonly IMacroBoard macroBoard;
    private readonly bool leaveOpen;

    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroBoardAdapter"/> class.
    /// </summary>
    /// <remarks>
    /// <para>When this instance is disposed, the underlying board is disposed as well.</para>
    /// </remarks>
    /// <param name="macroBoard">The macroBoard that is wrapped.</param>
    protected MacroBoardAdapter(IMacroBoard macroBoard)
        : this(macroBoard, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MacroBoardAdapter"/> class.
    /// </summary>
    /// <param name="macroBoard">The macroBoard that is wrapped.</param>
    /// <param name="leaveOpen">When true, the underlying macroBoard will not be disposed with this instance.</param>
    protected MacroBoardAdapter(IMacroBoard macroBoard, bool leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(macroBoard);

        this.macroBoard = macroBoard;
        this.leaveOpen = leaveOpen;

        macroBoard.ConnectionStateChanged += OnConnectionStateChanged;
        macroBoard.KeyStateChanged += OnKeyStateChanged;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="MacroBoardAdapter"/> class.
    /// </summary>
    ~MacroBoardAdapter()
    {
        Dispose(false);
    }

    /// <inheritdoc/>
    public event EventHandler<KeyEventArgs>? KeyStateChanged;

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs>? ConnectionStateChanged;

    /// <inheritdoc/>
    public virtual IKeyLayout Keys => macroBoard.Keys;

    /// <inheritdoc/>
    public virtual bool IsConnected => macroBoard.IsConnected;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public virtual void SetBrightness(byte percent)
    {
        macroBoard.SetBrightness(percent);
    }

    /// <inheritdoc/>
    public virtual void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        macroBoard.SetKeyBitmap(keyId, bitmapData);
    }

    /// <inheritdoc/>
    public virtual void ShowLogo()
    {
        macroBoard.ShowLogo();
    }

    /// <inheritdoc/>
    public string GetFirmwareVersion()
    {
        return macroBoard.GetFirmwareVersion();
    }

    /// <inheritdoc/>
    public string GetSerialNumber()
    {
        return macroBoard.GetFirmwareVersion();
    }

    /// <summary>
    /// Virtual KeyStateChanged event handler.
    /// </summary>
    /// <param name="sender">The sender of the original event.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnKeyStateChanged(object? sender, KeyEventArgs e)
    {
        KeyStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Virtual ConnectionStateChanged event handler.
    /// </summary>
    /// <param name="sender">The sender of the original event.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnConnectionStateChanged(object? sender, ConnectionEventArgs e)
    {
        ConnectionStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/> and false when called from the finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing && !leaveOpen)
        {
            macroBoard?.Dispose();
        }

        disposed = true;
    }
}
