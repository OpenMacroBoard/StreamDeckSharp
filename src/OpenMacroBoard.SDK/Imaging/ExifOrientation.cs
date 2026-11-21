using System;
using System.Buffers.Binary;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Reads and applies the EXIF orientation: cameras and phones store photos the way the sensor
/// sits and add a note how to turn the image for display.
/// </summary>
internal static class ExifOrientation
{
    /// <summary>
    /// The image is stored the way it is displayed.
    /// </summary>
    public const int Normal = 1;

    private const int MirrorHorizontal = 2;
    private const int Rotate180 = 3;
    private const int MirrorVertical = 4;
    private const int Transpose = 5;
    private const int Rotate90 = 6;
    private const int Transverse = 7;
    private const int Rotate270 = 8;

    private const int TiffHeaderLength = 8;
    private const int TiffMagicNumber = 42;
    private const int DirectoryEntryLength = 12;
    private const int OrientationTag = 0x0112;
    private const int TypeShort = 3;

    /// <summary>
    /// Reads the orientation from the content of a JPEG APP1 segment.
    /// </summary>
    /// <returns>
    /// A value from 1 to 8, or <see cref="Normal"/> if the segment holds no EXIF data, no
    /// orientation, or data that cannot be read. This method never throws for bad data: a
    /// damaged note about the orientation is no reason to reject the image.
    /// </returns>
    public static int ReadFromJpegSegment(ReadOnlySpan<byte> segment)
    {
        ReadOnlySpan<byte> exifHeader = "Exif\0\0"u8;

        if (!segment.StartsWith(exifHeader))
        {
            // APP1 is also used for other data, for example XMP
            return Normal;
        }

        // What follows is a TIFF structure. All offsets in it count from its first byte.
        var tiff = segment[exifHeader.Length..];

        if (tiff.Length < TiffHeaderLength)
        {
            return Normal;
        }

        bool littleEndian;

        if (tiff[0] == 'I' && tiff[1] == 'I')
        {
            littleEndian = true;
        }
        else if (tiff[0] == 'M' && tiff[1] == 'M')
        {
            littleEndian = false;
        }
        else
        {
            return Normal;
        }

        if (ReadUInt16(tiff, 2, littleEndian) != TiffMagicNumber)
        {
            return Normal;
        }

        // The first directory holds the tags of the main image.
        long directory = ReadUInt32(tiff, 4, littleEndian);

        if (directory + 2 > tiff.Length)
        {
            return Normal;
        }

        var entryCount = ReadUInt16(tiff, (int)directory, littleEndian);

        for (var i = 0; i < entryCount; i++)
        {
            var entry = directory + 2 + ((long)i * DirectoryEntryLength);

            if (entry + DirectoryEntryLength > tiff.Length)
            {
                return Normal;
            }

            var position = (int)entry;

            if (ReadUInt16(tiff, position, littleEndian) != OrientationTag)
            {
                continue;
            }

            var type = ReadUInt16(tiff, position + 2, littleEndian);
            var count = ReadUInt32(tiff, position + 4, littleEndian);
            var value = ReadUInt16(tiff, position + 8, littleEndian);

            return type == TypeShort && count == 1 && value is >= Normal and <= Rotate270 ? value : Normal;
        }

        return Normal;
    }

    /// <summary>
    /// Returns the image the way it is meant to be displayed.
    /// </summary>
    public static DecodedImage Apply(DecodedImage image, int orientation)
    {
        if (orientation is < MirrorHorizontal or > Rotate270)
        {
            return image;
        }

        var sourceWidth = image.Width;
        var sourceHeight = image.Height;
        var swapsSides = orientation >= Transpose;
        var result = swapsSides ? new DecodedImage(sourceHeight, sourceWidth) : new DecodedImage(sourceWidth, sourceHeight);

        ReadOnlySpan<byte> source = image.Bgra;
        Span<byte> target = result.Bgra;

        for (var y = 0; y < result.Height; y++)
        {
            for (var x = 0; x < result.Width; x++)
            {
                // Where the pixel (x, y) of the result comes from.
                var (sourceX, sourceY) = orientation switch
                {
                    MirrorHorizontal => (sourceWidth - 1 - x, y),
                    Rotate180 => (sourceWidth - 1 - x, sourceHeight - 1 - y),
                    MirrorVertical => (x, sourceHeight - 1 - y),
                    Transpose => (y, x),
                    Rotate90 => (y, sourceHeight - 1 - x),
                    Transverse => (sourceWidth - 1 - y, sourceHeight - 1 - x),
                    _ => (sourceWidth - 1 - y, x),
                };

                source
                    .Slice(((sourceY * sourceWidth) + sourceX) * DecodedImage.ChannelCount, DecodedImage.ChannelCount)
                    .CopyTo(target.Slice(((y * result.Width) + x) * DecodedImage.ChannelCount, DecodedImage.ChannelCount));
            }
        }

        return result;
    }

    private static int ReadUInt16(ReadOnlySpan<byte> data, int position, bool littleEndian)
    {
        var bytes = data.Slice(position, sizeof(ushort));
        return littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int position, bool littleEndian)
    {
        var bytes = data.Slice(position, sizeof(uint));
        return littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}
