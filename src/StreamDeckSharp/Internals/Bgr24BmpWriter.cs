using OpenMacroBoard.SDK;
using System;
using System.Buffers.Binary;

namespace StreamDeckSharp.Internals;

/// <summary>
/// Writes key bitmaps as uncompressed 24 bit BMP files, the format the older Stream Decks expect.
/// </summary>
internal static class Bgr24BmpWriter
{
    private const int FileHeaderSize = 14;
    private const int InfoHeaderSize = 40;
    private const int HeaderSize = FileHeaderSize + InfoHeaderSize;
    private const int ChannelCount = 3;
    private const int RowAlignment = 4;

    /// <summary>
    /// 96 dots per inch, expressed as pixels per meter.
    /// </summary>
    private const int PixelsPerMeter = 3780;

    public static byte[] Write(KeyBitmap keyBitmap)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);

        var width = keyBitmap.Width;
        var height = keyBitmap.Height;
        var pixels = ((IKeyBitmapDataAccess)keyBitmap).GetData().Span;

        var rowLength = width * ChannelCount;

        // every row is padded to a multiple of four bytes
        var paddedRowLength = (rowLength + RowAlignment - 1) / RowAlignment * RowAlignment;
        var imageSize = checked(paddedRowLength * height);

        var file = new byte[HeaderSize + imageSize];
        var header = file.AsSpan(0, HeaderSize);

        // file header
        header[0] = (byte)'B';
        header[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(header[2..], file.Length);
        BinaryPrimitives.WriteInt32LittleEndian(header[10..], HeaderSize);

        // info header
        BinaryPrimitives.WriteInt32LittleEndian(header[14..], InfoHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(header[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(header[22..], height);
        BinaryPrimitives.WriteInt16LittleEndian(header[26..], 1); // color planes
        BinaryPrimitives.WriteInt16LittleEndian(header[28..], ChannelCount * 8); // bits per pixel
        BinaryPrimitives.WriteInt32LittleEndian(header[34..], imageSize);
        BinaryPrimitives.WriteInt32LittleEndian(header[38..], PixelsPerMeter);
        BinaryPrimitives.WriteInt32LittleEndian(header[42..], PixelsPerMeter);

        // A BMP file stores the rows from bottom to top.
        for (var y = 0; y < height; y++)
        {
            var sourceRow = pixels.Slice((height - 1 - y) * rowLength, rowLength);
            sourceRow.CopyTo(file.AsSpan(HeaderSize + (y * paddedRowLength), rowLength));
        }

        return file;
    }
}
