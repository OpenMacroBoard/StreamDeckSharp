using OpenMacroBoard.SDK;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// A view model for a virtual macro board
/// </summary>
internal sealed class VirtualBoardViewModel : INotifyPropertyChanged, IMacroBoard
{
    private readonly Dispatcher dispatcher;
    private readonly bool[] currentKeyState;

    /// <summary>
    /// Constructs a new view model for a virtual macro board.
    /// </summary>
    /// <param name="keyLayout"></param>
    public VirtualBoardViewModel(GridKeyLayout keyLayout)
    {
        ArgumentNullException.ThrowIfNull(keyLayout);

        Keys = keyLayout;
        KeyImages = new KeyImageCollection(Keys.Count);
        currentKeyState = new bool[Keys.Count];

        dispatcher = Dispatcher.CurrentDispatcher;
        ShowLogo();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event EventHandler<KeyEventArgs>? KeyStateChanged;

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs>? ConnectionStateChanged;

    /// <inheritdoc/>
    public IKeyLayout Keys { get; }

    /// <inheritdoc/>
    public KeyImageCollection KeyImages { get; }

    /// <inheritdoc/>
    public bool IsConnected
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(value));
        }
    } = true;

    /// <inheritdoc/>
    public void SetBrightness(byte percent)
    {
        // The virtual board doesn't support brightness (yet), so we do nothing.
    }

    /// <inheritdoc/>
    public void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        IKeyBitmapDataAccess srcData = bitmapData;

        var wb = new WriteableBitmap(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null);

        var data = srcData.ToImage().ToBgr24PixelArray();
        var sourceStride = bitmapData.Width * 3;

        wb.WritePixels(new Int32Rect(0, 0, bitmapData.Width, bitmapData.Height), data, sourceStride, 0);

        wb.Freeze();

        KeyImages[keyId] = wb;
        RaiseKeyImagesChanges();
    }

    /// <inheritdoc/>
    public void ShowLogo()
    {
        // The virtual board doesn't support showing a logo (yet), so we do nothing.
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose
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

    internal bool GetKeyState(int keyId)
    {
        return currentKeyState[keyId];
    }

    internal void SendKeyState(int keyId, bool down)
    {
        if (currentKeyState[keyId] == down)
        {
            // same state do not notify subscribers.
            return;
        }

        currentKeyState[keyId] = down;
        KeyStateChanged?.Invoke(this, new KeyEventArgs(keyId, down));
    }

    private void RaiseKeyImagesChanges()
    {
        dispatcher.BeginInvoke(new Action(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeyImages)))));
    }
}
