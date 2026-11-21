using OpenMacroBoard.SDK;
using System;
using System.Text;

namespace OpenMacroBoard.Tests;

public sealed class FakeMacroBoard : IMacroBoard
{
    private readonly GridKeyLayout keyPosition;
    private const int ChannelCount = 3;

    private readonly KeyBitmap logoImage;
    private readonly byte[] boardPixels;
    private readonly int boardWidth;
    private readonly int boardHeight;

    private byte brightness = 100;
    private bool isLogoShown = false;
    private bool disposed = false;

    public FakeMacroBoard(GridKeyLayout keyPosition)
    {
        ArgumentNullException.ThrowIfNull(keyPosition);
        this.keyPosition = keyPosition;

        // the keys with a black border of one gap around them
        boardWidth = keyPosition.Area.Width + (2 * keyPosition.GapSize);
        boardHeight = keyPosition.Area.Height + (2 * keyPosition.GapSize);
        boardPixels = new byte[boardWidth * boardHeight * ChannelCount];

        logoImage = CreateLogoImage();

        ShowLogo();
    }

    public IKeyLayout Keys => keyPosition;

    public bool IsConnected
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            // Order when isConnected is set is important
            // because method calls might not work when the state
            // is set to "disconnected"

            if (!field)
            {
                field = true;
                ShowLogo();
            }
            else
            {
                this.ClearKeys();
                field = false;
            }

            ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(field));
        }
    } = true;

    public KeyBitmap BoardImage => KeyBitmap.Create.FromBgr24Array(boardWidth, boardHeight, boardPixels);

    public event EventHandler<KeyEventArgs> KeyStateChanged;
    public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        disposed = true;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="percent"/> is grater than 100%.</exception>
    public void SetBrightness(byte percent)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!IsConnected)
        {
            return;
        }

        if (percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        brightness = percent;
    }

    public void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!IsConnected)
        {
            return;
        }

        if (isLogoShown)
        {
            // set isLogoShown to false before clearing everything,
            // because clearing internally calls SetKeyBitmap, which would
            // cause a StackOverflow exception if isLogoShown is still true
            isLogoShown = false;
            this.ClearKeys();
        }

        ResizeAndApplyImage(keyId, bitmapData);
    }

    public void TriggerKeyEvent(int keyId, bool down)
    {
        if (!IsConnected)
        {
            return;
        }

        KeyStateChanged?.Invoke(this, new KeyEventArgs(keyId, down));
    }

    public void ShowLogo()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!IsConnected)
        {
            return;
        }

        this.DrawFullScreenBitmap(logoImage);

        // set isLogoShown after drawing the image, 
        // because drawing the image calls SetKeyBitmap
        // which internally sets isLogoShown to false
        isLogoShown = true;
    }

    public string GetMetaData()
    {
        var builder = new StringBuilder();

        builder.Append("IsConnected:   ").Append(IsConnected).AppendLine();

        if (IsConnected)
        {
            builder
                .Append("Brightness:    ").Append(brightness).AppendLine()
                .Append("IsLogoShown:   ").Append(isLogoShown).AppendLine()
                ;
        }

        return builder.ToString();
    }

    public string GetFirmwareVersion()
    {
        return "fake-firmware";
    }

    public string GetSerialNumber()
    {
        return "fake-serial-number";
    }

    /// <summary>
    /// Creates the logo in the size of all keys together: scaled to 95% of that area and centered on black.
    /// </summary>
    private KeyBitmap CreateLogoImage()
    {
        var logo = AssetLoader.LoadBitmap("OpenMacroBoard-Logo.png");
        var logoPixels = ((IKeyBitmapDataAccess)logo).GetData().Span;

        const double scalar = 0.95;

        var areaWidth = keyPosition.Area.Width;
        var areaHeight = keyPosition.Area.Height;

        var scaleFactorY = areaHeight * scalar / logo.Height;
        var scaleFactorX = areaWidth * scalar / logo.Width;
        var scaleFactor = Math.Min(scaleFactorX, scaleFactorY);

        var newHeight = (int)Math.Round(logo.Height * scaleFactor);
        var newWidth = (int)Math.Round(logo.Width * scaleFactor);

        var leftOffset = (areaWidth - newWidth) / 2;
        var topOffset = (areaHeight - newHeight) / 2;

        // Scaled by picking the nearest pixel, without smoothing.
        var widthFactor = logo.Width / (float)newWidth;
        var heightFactor = logo.Height / (float)newHeight;

        var total = new byte[areaWidth * areaHeight * ChannelCount];

        for (var y = 0; y < newHeight; y++)
        {
            var sourceY = (int)(y * heightFactor);

            for (var x = 0; x < newWidth; x++)
            {
                var sourceX = (int)(x * widthFactor);

                var from = ((sourceY * logo.Width) + sourceX) * ChannelCount;
                var to = (((y + topOffset) * areaWidth) + x + leftOffset) * ChannelCount;

                logoPixels.Slice(from, ChannelCount).CopyTo(total.AsSpan(to, ChannelCount));
            }
        }

        return KeyBitmap.Create.FromBgr24Array(areaWidth, areaHeight, total);
    }

    private void ResizeAndApplyImage(int keyId, KeyBitmap image)
    {
        // resize to target key size (returns the same bitmap if it already has that size)
        var keySize = keyPosition.KeySize;
        var pixels = ((IKeyBitmapDataAccess)image.Resize(keySize, keySize)).GetData().Span;

        // draw to target position
        var targetPosition = keyPosition[keyId];
        var left = targetPosition.X + keyPosition.GapSize;
        var top = targetPosition.Y + keyPosition.GapSize;

        for (var y = 0; y < keySize; y++)
        {
            var row = pixels.Slice(y * keySize * ChannelCount, keySize * ChannelCount);
            row.CopyTo(boardPixels.AsSpan((((top + y) * boardWidth) + left) * ChannelCount));
        }
    }
}
