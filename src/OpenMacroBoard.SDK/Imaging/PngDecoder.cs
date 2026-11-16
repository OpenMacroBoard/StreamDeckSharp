using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Decodes PNG files: all color types and bit depths, with and without interlacing.
/// </summary>
/// <remarks>
/// <para>Not supported, and silently ignored: animation, color profiles and gamma.
/// Samples with 16 bits are reduced to 8 bits.</para>
/// <para>The pixel data of a PNG file is compressed with zlib; decompression is done by .NET.</para>
/// </remarks>
internal static class PngDecoder
{
    private const int ChunkOverhead = 12;
    private const int HeaderLength = 13;
    private const int Opaque = 255;

    private const int ColorTypeGray = 0;
    private const int ColorTypeRgb = 2;
    private const int ColorTypePalette = 3;
    private const int ColorTypeGrayAlpha = 4;
    private const int ColorTypeRgbAlpha = 6;

    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// The seven passes of Adam7 interlacing: first column, first row, column step, row step.
    /// </summary>
    private static ReadOnlySpan<byte> InterlacePasses =>
    [
        0, 0, 8, 8,
        4, 0, 8, 8,
        0, 4, 4, 8,
        2, 0, 4, 4,
        0, 2, 2, 4,
        1, 0, 2, 2,
        0, 1, 1, 2,
    ];

    public static bool IsPng(ReadOnlySpan<byte> data)
    {
        return data.StartsWith(Signature);
    }

    public static DecodedImage Decode(ReadOnlySpan<byte> data)
    {
        Header? header = null;
        ReadOnlySpan<byte> palette = default;
        ReadOnlySpan<byte> transparency = default;
        using var compressed = new MemoryStream();

        var position = Signature.Length;
        var finished = false;

        while (!finished && position + ChunkOverhead <= data.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data[position..]);

            if (length > data.Length - position - ChunkOverhead)
            {
                throw new ImageDecodingException("The PNG file is incomplete.");
            }

            var type = data.Slice(position + 4, 4);
            var content = data.Slice(position + 8, (int)length);
            position += ChunkOverhead + (int)length;

            if (type.SequenceEqual("IHDR"u8))
            {
                header = ReadHeader(content);
            }
            else if (type.SequenceEqual("PLTE"u8))
            {
                palette = content;
            }
            else if (type.SequenceEqual("tRNS"u8))
            {
                transparency = content;
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                compressed.Write(content);
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                finished = true;
            }
        }

        if (header is not { } info)
        {
            throw new ImageDecodingException("The PNG file has no header.");
        }

        if (info.ColorType == ColorTypePalette && palette.IsEmpty)
        {
            throw new ImageDecodingException("The PNG file has no palette.");
        }

        var image = new DecodedImage(info.Width, info.Height);
        var raw = Decompress(compressed, info);

        var offset = 0;

        if (info.Interlaced)
        {
            for (var pass = 0; pass < InterlacePasses.Length; pass += 4)
            {
                int firstX = InterlacePasses[pass];
                int firstY = InterlacePasses[pass + 1];
                int stepX = InterlacePasses[pass + 2];
                int stepY = InterlacePasses[pass + 3];

                var passWidth = GetPassSize(info.Width, firstX, stepX);
                var passHeight = GetPassSize(info.Height, firstY, stepY);

                offset = DecodePass(image, info, raw, offset, palette, transparency, new Pass(firstX, firstY, stepX, stepY, passWidth, passHeight));
            }
        }
        else
        {
            DecodePass(image, info, raw, offset, palette, transparency, new Pass(0, 0, 1, 1, info.Width, info.Height));
        }

        return image;
    }

    private static Header ReadHeader(ReadOnlySpan<byte> content)
    {
        if (content.Length != HeaderLength)
        {
            throw new ImageDecodingException("The PNG header is invalid.");
        }

        var width = BinaryPrimitives.ReadUInt32BigEndian(content);
        var height = BinaryPrimitives.ReadUInt32BigEndian(content[4..]);
        int bitDepth = content[8];
        int colorType = content[9];
        int interlace = content[12];

        DecodedImage.EnsureValidSize(width, height);

        var validDepth = colorType switch
        {
            ColorTypeGray => bitDepth is 1 or 2 or 4 or 8 or 16,
            ColorTypePalette => bitDepth is 1 or 2 or 4 or 8,
            ColorTypeRgb or ColorTypeGrayAlpha or ColorTypeRgbAlpha => bitDepth is 8 or 16,
            _ => false,
        };

        if (!validDepth || content[10] != 0 || content[11] != 0 || interlace > 1)
        {
            throw new ImageDecodingException("The PNG header is invalid.");
        }

        var channels = colorType switch
        {
            ColorTypeRgb => 3,
            ColorTypeGrayAlpha => 2,
            ColorTypeRgbAlpha => 4,
            _ => 1,
        };

        return new Header((int)width, (int)height, bitDepth, colorType, channels, interlace == 1);
    }

    private static int GetPassSize(int size, int first, int step)
    {
        return size <= first ? 0 : (size - first + step - 1) / step;
    }

    private static int GetRowLength(Header info, int width)
    {
        return checked((int)((((long)width * info.Channels * info.BitDepth) + 7) / 8));
    }

    /// <summary>
    /// Decompresses the pixel data. The size is known from the header, so never more than that is read.
    /// </summary>
    private static byte[] Decompress(MemoryStream compressed, Header info)
    {
        long size = 0;

        if (info.Interlaced)
        {
            for (var pass = 0; pass < InterlacePasses.Length; pass += 4)
            {
                var passWidth = GetPassSize(info.Width, InterlacePasses[pass], InterlacePasses[pass + 2]);
                var passHeight = GetPassSize(info.Height, InterlacePasses[pass + 1], InterlacePasses[pass + 3]);

                if (passWidth > 0)
                {
                    size += (long)passHeight * (1 + GetRowLength(info, passWidth));
                }
            }
        }
        else
        {
            size = (long)info.Height * (1 + GetRowLength(info, info.Width));
        }

        var raw = new byte[checked((int)size)];

        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        zlib.ReadExactly(raw);

        return raw;
    }

    /// <summary>
    /// Decodes the rows of one pass (the whole image, if it is not interlaced).
    /// </summary>
    /// <returns>The offset in <paramref name="raw"/> after this pass.</returns>
    private static int DecodePass(
        DecodedImage image,
        Header info,
        byte[] raw,
        int offset,
        ReadOnlySpan<byte> palette,
        ReadOnlySpan<byte> transparency,
        Pass pass
    )
    {
        if (pass.Width == 0 || pass.Height == 0)
        {
            return offset;
        }

        var rowLength = GetRowLength(info, pass.Width);
        var bytesPerPixel = Math.Max(1, info.Channels * info.BitDepth / 8);

        Span<byte> previous = new byte[rowLength];

        for (var y = 0; y < pass.Height; y++)
        {
            var filter = raw[offset];
            var row = raw.AsSpan(offset + 1, rowLength);
            offset += 1 + rowLength;

            Unfilter(filter, row, previous, bytesPerPixel);

            var targetY = pass.FirstY + (y * pass.StepY);

            for (var x = 0; x < pass.Width; x++)
            {
                SetPixel(image, info, row, x, pass.FirstX + (x * pass.StepX), targetY, palette, transparency);
            }

            previous = row;
        }

        return offset;
    }

    /// <summary>
    /// Reverses the filter of a row. Every byte was stored as the difference to a prediction
    /// that is made from the bytes to the left and above.
    /// </summary>
    /// <exception cref="ImageDecodingException">The filter type is unknown.</exception>
    private static void Unfilter(byte filter, Span<byte> row, ReadOnlySpan<byte> previous, int bytesPerPixel)
    {
        for (var i = 0; i < row.Length; i++)
        {
            int left = i >= bytesPerPixel ? row[i - bytesPerPixel] : 0;
            int above = previous[i];
            int aboveLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;

            var prediction = filter switch
            {
                0 => 0,
                1 => left,
                2 => above,
                3 => (left + above) / 2,
                4 => Paeth(left, above, aboveLeft),
                _ => throw new ImageDecodingException("The PNG file uses an unknown row filter."),
            };

            row[i] = (byte)(row[i] + prediction);
        }
    }

    private static int Paeth(int left, int above, int aboveLeft)
    {
        var estimate = left + above - aboveLeft;
        var distanceLeft = Math.Abs(estimate - left);
        var distanceAbove = Math.Abs(estimate - above);
        var distanceAboveLeft = Math.Abs(estimate - aboveLeft);

        if (distanceLeft <= distanceAbove && distanceLeft <= distanceAboveLeft)
        {
            return left;
        }

        return distanceAbove <= distanceAboveLeft ? above : aboveLeft;
    }

    /// <summary>
    /// Reads one sample (one channel of one pixel) with its full bit depth.
    /// </summary>
    private static int ReadSample(Header info, ReadOnlySpan<byte> row, int sampleIndex)
    {
        switch (info.BitDepth)
        {
            case 8:
                return row[sampleIndex];

            case 16:
                return BinaryPrimitives.ReadUInt16BigEndian(row[(sampleIndex * 2)..]);

            default:
                var bitPosition = sampleIndex * info.BitDepth;
                var shift = 8 - info.BitDepth - (bitPosition % 8);
                return (row[bitPosition / 8] >> shift) & ((1 << info.BitDepth) - 1);
        }
    }

    /// <summary>
    /// Scales a sample to 0 - 255.
    /// </summary>
    private static byte ToByte(Header info, int sample)
    {
        return info.BitDepth switch
        {
            8 => (byte)sample,
            16 => (byte)(sample >> 8),
            _ => (byte)(sample * Opaque / ((1 << info.BitDepth) - 1)),
        };
    }

    private static void SetPixel(
        DecodedImage image,
        Header info,
        ReadOnlySpan<byte> row,
        int x,
        int targetX,
        int targetY,
        ReadOnlySpan<byte> palette,
        ReadOnlySpan<byte> transparency
    )
    {
        var first = x * info.Channels;

        switch (info.ColorType)
        {
            case ColorTypePalette:
                SetPalettePixel(image, ReadSample(info, row, first), targetX, targetY, palette, transparency);
                break;

            case ColorTypeGray:
                SetGrayPixel(image, info, ReadSample(info, row, first), targetX, targetY, transparency);
                break;

            case ColorTypeGrayAlpha:
                var gray = ToByte(info, ReadSample(info, row, first));
                var grayAlpha = ToByte(info, ReadSample(info, row, first + 1));

                image.SetPixel(targetX, targetY, gray, gray, gray, grayAlpha);
                break;

            default:
                SetRgbPixel(image, info, row, first, targetX, targetY, transparency);
                break;
        }
    }

    private static void SetPalettePixel(
        DecodedImage image,
        int index,
        int targetX,
        int targetY,
        ReadOnlySpan<byte> palette,
        ReadOnlySpan<byte> transparency
    )
    {
        var entry = palette.Slice(index * 3, 3);
        var alpha = index < transparency.Length ? transparency[index] : (byte)Opaque;

        image.SetPixel(targetX, targetY, entry[0], entry[1], entry[2], alpha);
    }

    private static void SetGrayPixel(
        DecodedImage image,
        Header info,
        int sample,
        int targetX,
        int targetY,
        ReadOnlySpan<byte> transparency
    )
    {
        var gray = ToByte(info, sample);

        // One gray value can be marked as fully transparent.
        var isTransparent = transparency.Length >= 2 && sample == BinaryPrimitives.ReadUInt16BigEndian(transparency);

        image.SetPixel(targetX, targetY, gray, gray, gray, isTransparent ? (byte)0 : (byte)Opaque);
    }

    private static void SetRgbPixel(
        DecodedImage image,
        Header info,
        ReadOnlySpan<byte> row,
        int first,
        int targetX,
        int targetY,
        ReadOnlySpan<byte> transparency
    )
    {
        var red = ReadSample(info, row, first);
        var green = ReadSample(info, row, first + 1);
        var blue = ReadSample(info, row, first + 2);

        byte alpha = Opaque;

        if (info.ColorType == ColorTypeRgbAlpha)
        {
            alpha = ToByte(info, ReadSample(info, row, first + 3));
        }
        else if (
            transparency.Length >= 6
            && red == BinaryPrimitives.ReadUInt16BigEndian(transparency)
            && green == BinaryPrimitives.ReadUInt16BigEndian(transparency[2..])
            && blue == BinaryPrimitives.ReadUInt16BigEndian(transparency[4..])
        )
        {
            // One color can be marked as fully transparent.
            alpha = 0;
        }

        image.SetPixel(targetX, targetY, ToByte(info, red), ToByte(info, green), ToByte(info, blue), alpha);
    }

    private readonly record struct Header(int Width, int Height, int BitDepth, int ColorType, int Channels, bool Interlaced);

    private readonly record struct Pass(int FirstX, int FirstY, int StepX, int StepY, int Width, int Height);
}
