using System;
using System.Buffers.Binary;
using System.Numerics;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Decodes uncompressed BMP files (1, 4, 8, 16, 24 and 32 bits per pixel).
/// </summary>
internal static class BmpDecoder
{
    private const int FileHeaderSize = 14;
    private const int CoreHeaderSize = 12;
    private const int InfoHeaderSize = 40;
    private const int MaskSize = 4;
    private const int RowAlignment = 4;
    private const int Opaque = 255;

    private const int CompressionNone = 0;
    private const int CompressionBitFields = 3;
    private const int CompressionAlphaBitFields = 6;

    public static bool IsBmp(ReadOnlySpan<byte> data)
    {
        return data.StartsWith("BM"u8);
    }

    public static DecodedImage Decode(ReadOnlySpan<byte> data)
    {
        var pixelOffset = ReadInt32(data, 10);
        var headerSize = ReadInt32(data, FileHeaderSize);
        var header = data.Slice(FileHeaderSize, headerSize);

        int width;
        int signedHeight;
        int bitsPerPixel;
        var compression = CompressionNone;
        var colorsUsed = 0;
        var paletteEntrySize = 4;

        if (headerSize == CoreHeaderSize)
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(header[4..]);
            signedHeight = BinaryPrimitives.ReadUInt16LittleEndian(header[6..]);
            bitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(header[10..]);
            paletteEntrySize = 3;
        }
        else if (headerSize >= InfoHeaderSize)
        {
            width = ReadInt32(header, 4);
            signedHeight = ReadInt32(header, 8);
            bitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(header[14..]);
            compression = ReadInt32(header, 16);
            colorsUsed = ReadInt32(header, 32);
        }
        else
        {
            throw new ImageDecodingException("Unsupported BMP header.");
        }

        if (compression is not (CompressionNone or CompressionBitFields or CompressionAlphaBitFields))
        {
            throw new ImageDecodingException("Compressed BMP files are not supported.");
        }

        // A negative height means that the rows are stored from top to bottom.
        var topDown = signedHeight < 0;
        var height = Math.Abs((long)signedHeight);

        var image = new DecodedImage(width, (int)height);
        var afterHeader = FileHeaderSize + headerSize;

        var masks = ReadMasks(data, headerSize, bitsPerPixel, compression, ref afterHeader);

        ReadOnlySpan<byte> palette = default;

        if (bitsPerPixel <= 8)
        {
            var colorCount = colorsUsed is > 0 and <= 256 ? colorsUsed : 1 << bitsPerPixel;
            palette = data.Slice(afterHeader, colorCount * paletteEntrySize);
        }

        var rowLength = checked((((long)width * bitsPerPixel) + 31) / 32 * RowAlignment);

        for (var y = 0; y < image.Height; y++)
        {
            var storedRow = topDown ? y : image.Height - 1 - y;
            var row = data.Slice(checked((int)(pixelOffset + (storedRow * rowLength))), (int)rowLength);

            switch (bitsPerPixel)
            {
                case 1 or 4 or 8:
                    DecodePaletteRow(image, y, row, bitsPerPixel, palette, paletteEntrySize);
                    break;

                case 24:
                    for (var x = 0; x < width; x++)
                    {
                        image.SetPixel(x, y, row[(x * 3) + 2], row[(x * 3) + 1], row[x * 3], Opaque);
                    }

                    break;

                case 16:
                    for (var x = 0; x < width; x++)
                    {
                        SetMaskedPixel(image, x, y, BinaryPrimitives.ReadUInt16LittleEndian(row[(x * 2)..]), masks);
                    }

                    break;

                case 32:
                    for (var x = 0; x < width; x++)
                    {
                        SetMaskedPixel(image, x, y, BinaryPrimitives.ReadUInt32LittleEndian(row[(x * 4)..]), masks);
                    }

                    break;

                default:
                    throw new ImageDecodingException($"BMP files with {bitsPerPixel} bits per pixel are not supported.");
            }
        }

        return image;
    }

    private static int ReadInt32(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
    }

    /// <summary>
    /// Reads the bit masks that say where red, green, blue and alpha are stored in a 16 or 32 bit pixel.
    /// </summary>
    private static ChannelMasks ReadMasks(
        ReadOnlySpan<byte> data,
        int headerSize,
        int bitsPerPixel,
        int compression,
        ref int afterHeader
    )
    {
        if (compression == CompressionNone)
        {
            // the default layouts: 5 bits per channel for 16 bit, 8 bits per channel without alpha for 32 bit
            return bitsPerPixel == 16
                ? new ChannelMasks(0x7C00, 0x03E0, 0x001F, 0)
                : new ChannelMasks(0x00FF0000, 0x0000FF00, 0x000000FF, 0);
        }

        // The masks directly follow the 40 bytes of the basic header. Larger headers contain them.
        const int masksOffset = FileHeaderSize + InfoHeaderSize;
        var hasAlphaMask = compression == CompressionAlphaBitFields || headerSize >= InfoHeaderSize + (4 * MaskSize);
        var maskCount = hasAlphaMask ? 4 : 3;

        if (headerSize == InfoHeaderSize)
        {
            afterHeader += maskCount * MaskSize;
        }

        var masks = data.Slice(masksOffset, maskCount * MaskSize);

        return new ChannelMasks(
            BinaryPrimitives.ReadUInt32LittleEndian(masks),
            BinaryPrimitives.ReadUInt32LittleEndian(masks[MaskSize..]),
            BinaryPrimitives.ReadUInt32LittleEndian(masks[(2 * MaskSize)..]),
            hasAlphaMask ? BinaryPrimitives.ReadUInt32LittleEndian(masks[(3 * MaskSize)..]) : 0
        );
    }

    private static void DecodePaletteRow(
        DecodedImage image,
        int y,
        ReadOnlySpan<byte> row,
        int bitsPerPixel,
        ReadOnlySpan<byte> palette,
        int paletteEntrySize
    )
    {
        var mask = (1 << bitsPerPixel) - 1;

        for (var x = 0; x < image.Width; x++)
        {
            var bitPosition = x * bitsPerPixel;
            var shift = 8 - bitsPerPixel - (bitPosition % 8);
            var index = (row[bitPosition / 8] >> shift) & mask;

            var entry = palette.Slice(index * paletteEntrySize, 3);
            image.SetPixel(x, y, entry[2], entry[1], entry[0], Opaque);
        }
    }

    private static void SetMaskedPixel(DecodedImage image, int x, int y, uint value, ChannelMasks masks)
    {
        var alpha = masks.Alpha == 0 ? (byte)Opaque : Extract(value, masks.Alpha);

        image.SetPixel(
            x,
            y,
            Extract(value, masks.Red),
            Extract(value, masks.Green),
            Extract(value, masks.Blue),
            alpha
        );
    }

    /// <summary>
    /// Takes the bits of one channel out of a pixel value and scales them to 0 - 255.
    /// </summary>
    private static byte Extract(uint value, uint mask)
    {
        if (mask == 0)
        {
            return 0;
        }

        var shift = BitOperations.TrailingZeroCount(mask);
        var max = mask >> shift;

        // rounded to the nearest value
        return (byte)(((((value & mask) >> shift) * Opaque) + (max / 2)) / max);
    }

    private readonly record struct ChannelMasks(uint Red, uint Green, uint Blue, uint Alpha);
}
