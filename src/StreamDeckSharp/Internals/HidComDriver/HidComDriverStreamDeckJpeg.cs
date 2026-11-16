using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals.HidComDriver;

/// <summary>
/// HID Stream Deck communication driver for JPEG based devices.
/// </summary>
public sealed class HidComDriverStreamDeckJpeg
    : IStreamDeckHidComDriver
{
    /// <inheritdoc/>
    public int HeaderSize => 8;

    /// <inheritdoc/>
    public int ReportSize => 1024;

    /// <inheritdoc/>
    public int ExpectedFeatureReportLength => 32;

    /// <inheritdoc/>
    public int ExpectedOutputReportLength => 1024;

    /// <inheritdoc/>
    public int ExpectedInputReportLength => 512;

    /// <inheritdoc/>
    public int KeyReportOffset => 4;

    /// <inheritdoc/>
    public byte FirmwareVersionFeatureId => 5;

    /// <inheritdoc/>
    public byte SerialNumberFeatureId => 6;

    /// <inheritdoc/>
    public int FirmwareVersionReportSkip => 6;

    /// <inheritdoc/>
    public int SerialNumberReportSkip => 2;

    /// <inheritdoc/>
    public double BytesPerSecondLimit { get; init; } = double.PositiveInfinity;

    /// <inheritdoc/>
    public required int KeyImageSize { get; init; }

    /// <inheritdoc/>
    public IKeyIdMapper KeyIdMapper => CommonKeyMappers.Identity;

    /// <inheritdoc/>
    public byte[] GeneratePayload(KeyBitmap keyBitmap, IJpegEncoder jpegEncoder)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);
        ArgumentNullException.ThrowIfNull(jpegEncoder);

        var deviceBitmap = keyBitmap
            .Resize(KeyImageSize, KeyImageSize)
            .Rotate180();

        var pixels = ((IKeyBitmapDataAccess)deviceBitmap).GetData().Span;
        return jpegEncoder.Encode(pixels, deviceBitmap.Width, deviceBitmap.Height);
    }

    /// <inheritdoc/>
    public void PrepareDataForTransmission(
        byte[] data,
        int pageNumber,
        int payloadLength,
        int keyId,
        bool isLast
    )
    {
        data[0] = 2;
        data[1] = 7;
        data[2] = (byte)keyId;
        data[3] = (byte)(isLast ? 1 : 0);
        data[4] = (byte)(payloadLength & 255);
        data[5] = (byte)(payloadLength >> 8);
        data[6] = (byte)pageNumber;
    }

    /// <inheritdoc/>
    public byte[] GetBrightnessMessage(byte percent)
    {
        if (percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        var buffer = new byte[]
        {
            0x03, 0x08, 0x64, 0x23, 0xB8, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xA5, 0x49, 0xCD, 0x02, 0xFE, 0x7F, 0x00, 0x00,
        };

        buffer[2] = percent;
        buffer[3] = 0x23;  // 0x23, sometimes 0x27

        return buffer;
    }

    /// <inheritdoc/>
    public byte[] GetLogoMessage()
    {
        return [0x03, 0x02];
    }
}
