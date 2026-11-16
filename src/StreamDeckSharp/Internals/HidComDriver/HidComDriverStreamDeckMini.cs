using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals.HidComDriver;

/// <summary>
/// HID Stream Deck communication driver for the Stream Deck Mini.
/// </summary>
public sealed class HidComDriverStreamDeckMini
    : IStreamDeckHidComDriver
{
    /// <inheritdoc/>
    public int HeaderSize => 16;

    /// <inheritdoc/>
    public int ReportSize => 1024;

    /// <inheritdoc/>
    public int ExpectedFeatureReportLength => 17;

    /// <inheritdoc/>
    public int ExpectedOutputReportLength => 1024;

    /// <inheritdoc/>
    public int ExpectedInputReportLength => 17;

    /// <inheritdoc/>
    public int KeyReportOffset => 1;

    /// <inheritdoc/>
    public byte FirmwareVersionFeatureId => 4;

    /// <inheritdoc/>
    public byte SerialNumberFeatureId => 3;

    /// <inheritdoc/>
    public int FirmwareVersionReportSkip => 5;

    /// <inheritdoc/>
    public int SerialNumberReportSkip => 5;

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

        var deviceBitmap = keyBitmap
            .Resize(KeyImageSize, KeyImageSize)
            .Rotate270()
            .FlipVertical();

        return Bgr24BmpWriter.Write(deviceBitmap);
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
        data[0] = 2; // Report ID ?
        data[1] = 1; // ?
        data[2] = (byte)pageNumber;
        data[4] = (byte)(isLast ? 1 : 0);
        data[5] = (byte)(keyId + 1);
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
            0x05, 0x55, 0xaa, 0xd1, 0x01, 0x64, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00,
        };

        buffer[5] = percent;
        return buffer;
    }

    /// <inheritdoc/>
    public byte[] GetLogoMessage()
    {
        return [0x0B, 0x63];
    }
}
