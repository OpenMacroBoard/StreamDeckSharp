using OpenMacroBoard.SDK;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using VerifyTests;

namespace OpenMacroBoard.Meta.TestUtils;

/// <summary>
/// Image snapshots as PNG files. Two PNG files are equal if they decode to the same pixels,
/// no matter how they were compressed.
/// </summary>
public static class PngSnapshot
{
    private const int ChannelCount = 3;
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>
    /// Registers the pixel comparison for PNG snapshots. Has to be called once before verifying images.
    /// </summary>
    public static void Initialize()
    {
        VerifierSettings.RegisterStreamComparer("png", CompareAsync);
    }

    /// <summary>
    /// Writes a bitmap as PNG file (8 bit RGB, not interlaced).
    /// </summary>
    public static byte[] Encode(KeyBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var pixels = ((IKeyBitmapDataAccess)bitmap).GetData().Span;
        var rowLength = bitmap.Width * ChannelCount;

        // every row starts with its filter type (0 = none), followed by red, green, blue bytes
        var raw = new byte[bitmap.Height * (rowLength + 1)];

        for (var y = 0; y < bitmap.Height; y++)
        {
            var target = raw.AsSpan((y * (rowLength + 1)) + 1, rowLength);

            for (var x = 0; x < bitmap.Width; x++)
            {
                var source = ((y * bitmap.Width) + x) * ChannelCount;

                target[x * ChannelCount] = pixels[source + 2];
                target[(x * ChannelCount) + 1] = pixels[source + 1];
                target[(x * ChannelCount) + 2] = pixels[source];
            }
        }

        using var compressed = new MemoryStream();

        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, bitmap.Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), bitmap.Height);
        header[8] = 8; // bits per sample
        header[9] = 2; // color type RGB

        using var file = new MemoryStream();
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(file, "IHDR"u8, header);
        WriteChunk(file, "IDAT"u8, compressed.ToArray());
        WriteChunk(file, "IEND"u8, []);

        return file.ToArray();
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> content)
    {
        Span<byte> number = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(number, content.Length);
        output.Write(number);
        output.Write(type);
        output.Write(content);

        BinaryPrimitives.WriteUInt32BigEndian(number, UpdateCrc(UpdateCrc(0xFFFFFFFF, type), content) ^ 0xFFFFFFFF);
        output.Write(number);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for (uint n = 0; n < table.Length; n++)
        {
            var c = n;

            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static async Task<CompareResult> CompareAsync(
        Stream received,
        Stream verified,
        IReadOnlyDictionary<string, object> context
    )
    {
        var receivedBitmap = await KeyBitmap.Create.FromStreamAsync(received);
        var verifiedBitmap = await KeyBitmap.Create.FromStreamAsync(verified);

        if (receivedBitmap.Width != verifiedBitmap.Width || receivedBitmap.Height != verifiedBitmap.Height)
        {
            return CompareResult.NotEqual(
                $"The size differs: received {receivedBitmap.Width} x {receivedBitmap.Height}, " +
                $"verified {verifiedBitmap.Width} x {verifiedBitmap.Height}."
            );
        }

        var receivedPixels = ((IKeyBitmapDataAccess)receivedBitmap).GetData();
        var verifiedPixels = ((IKeyBitmapDataAccess)verifiedBitmap).GetData();

        var differing = 0;

        for (var i = 0; i < receivedPixels.Length; i += ChannelCount)
        {
            if (!receivedPixels.Span.Slice(i, ChannelCount).SequenceEqual(verifiedPixels.Span.Slice(i, ChannelCount)))
            {
                differing++;
            }
        }

        return differing == 0
            ? CompareResult.Equal
            : CompareResult.NotEqual($"{differing} of {receivedPixels.Length / ChannelCount} pixels differ.");
    }
}
