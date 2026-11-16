using OpenMacroBoard.SDK.Imaging;
using System;
using System.IO;

namespace OpenMacroBoard.SDK;

/// <summary>
/// The built-in JPEG encoder.
/// </summary>
/// <remarks>
/// <para>Writes baseline JPEG files (8 bit, YCbCr, 4:2:0 chroma subsampling, standard Huffman tables),
/// which every JPEG decoder can read.</para>
/// <para>The encoder is written for safety and simplicity, not for speed: it uses no unsafe code
/// and no native libraries. Instances are immutable and can be used from several threads.</para>
/// </remarks>
public sealed class OmbJpegEncoder : IJpegEncoder
{
    /// <summary>
    /// The quality that is used when no quality is given.
    /// </summary>
    public const int DefaultQuality = 75;

    private const int MaxDimension = 65535;
    private const int BlockSize = 8;
    private const int BlockLength = BlockSize * BlockSize;
    private const int McuSize = 16;
    private const int ChannelCount = 3;

    private readonly byte[] luminanceQuantization;
    private readonly byte[] chrominanceQuantization;

    private readonly JpegHuffmanTable dcLuminance = new(JpegTables.DcLuminanceCodeCounts, JpegTables.DcLuminanceSymbols);
    private readonly JpegHuffmanTable acLuminance = new(JpegTables.AcLuminanceCodeCounts, JpegTables.AcLuminanceSymbols);
    private readonly JpegHuffmanTable dcChrominance = new(JpegTables.DcChrominanceCodeCounts, JpegTables.DcChrominanceSymbols);
    private readonly JpegHuffmanTable acChrominance = new(JpegTables.AcChrominanceCodeCounts, JpegTables.AcChrominanceSymbols);

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbJpegEncoder"/> class
    /// with the default quality (<see cref="DefaultQuality"/>).
    /// </summary>
    public OmbJpegEncoder()
        : this(DefaultQuality)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbJpegEncoder"/> class.
    /// </summary>
    /// <param name="quality">
    /// The quality from 1 (smallest files) to 100 (best quality).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="quality"/> is smaller than 1 or larger than 100.
    /// </exception>
    public OmbJpegEncoder(int quality)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 100);

        Quality = quality;
        luminanceQuantization = ScaleQuantization(JpegTables.LuminanceQuantization, quality);
        chrominanceQuantization = ScaleQuantization(JpegTables.ChrominanceQuantization, quality);
    }

    /// <summary>
    /// Gets an encoder with the default quality (<see cref="DefaultQuality"/>).
    /// </summary>
    public static OmbJpegEncoder Default { get; } = new();

    /// <summary>
    /// Gets the quality from 1 (smallest files) to 100 (best quality).
    /// </summary>
    public int Quality { get; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either <paramref name="width"/> or <paramref name="height"/> are smaller than 1 or larger than 65535.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The length of <paramref name="bitmapDataBgr24"/> is not
    /// <paramref name="width"/> x <paramref name="height"/> x 3.
    /// </exception>
    public byte[] Encode(ReadOnlySpan<byte> bitmapDataBgr24, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaxDimension);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaxDimension);

        var expectedLength = (long)width * height * ChannelCount;

        if (bitmapDataBgr24.Length != expectedLength)
        {
            throw new ArgumentException($"{nameof(bitmapDataBgr24)}.Length does not match it's expected size ({nameof(width)} x {nameof(height)} x 3)", nameof(bitmapDataBgr24));
        }

        using var output = new MemoryStream();

        WriteHeaders(output, width, height);
        WriteScan(output, bitmapDataBgr24, width, height);
        WriteMarker(output, 0xD9); // end of image

        return output.ToArray();
    }

    /// <summary>
    /// Scales a quantization table to a quality, the same way the widely used libjpeg does.
    /// </summary>
    private static byte[] ScaleQuantization(ReadOnlySpan<byte> baseTable, int quality)
    {
        var scale = quality < 50 ? 5000 / quality : 200 - (quality * 2);
        var table = new byte[BlockLength];

        for (var i = 0; i < BlockLength; i++)
        {
            var value = ((baseTable[i] * scale) + 50) / 100;
            table[i] = (byte)Math.Clamp(value, 1, 255);
        }

        return table;
    }

    private static void WriteMarker(Stream output, byte marker)
    {
        output.WriteByte(0xFF);
        output.WriteByte(marker);
    }

    private static void WriteUInt16(Stream output, int value)
    {
        output.WriteByte((byte)(value >> 8));
        output.WriteByte((byte)value);
    }

    private static void WriteQuantizationTable(Stream output, byte tableId, ReadOnlySpan<byte> table)
    {
        output.WriteByte(tableId); // 8 bit precision, table id

        for (var i = 0; i < BlockLength; i++)
        {
            output.WriteByte(table[JpegTables.ZigZag[i]]);
        }
    }

    private static int GetHuffmanTableLength(JpegHuffmanTable table)
    {
        return 1 + table.CodeCounts.Length + table.Symbols.Length;
    }

    private static void WriteHuffmanTable(Stream output, byte classAndId, JpegHuffmanTable table)
    {
        output.WriteByte(classAndId);
        output.Write(table.CodeCounts);
        output.Write(table.Symbols);
    }

    private static void WriteFrameHeader(Stream output, int width, int height)
    {
        WriteMarker(output, 0xC0); // baseline
        WriteUInt16(output, 2 + 6 + (ChannelCount * 3));
        output.WriteByte(8); // bits per sample
        WriteUInt16(output, height);
        WriteUInt16(output, width);
        output.WriteByte(ChannelCount);

        // component id, sampling factors (horizontal << 4 | vertical), quantization table
        output.Write([1, 0x22, 0]); // Y, full resolution
        output.Write([2, 0x11, 1]); // Cb, half resolution in both directions
        output.Write([3, 0x11, 1]); // Cr, half resolution in both directions
    }

    private static void WriteScanHeader(Stream output)
    {
        WriteMarker(output, 0xDA);
        WriteUInt16(output, 2 + 1 + (ChannelCount * 2) + 3);
        output.WriteByte(ChannelCount);

        // component id, Huffman tables (DC << 4 | AC)
        output.Write([1, 0x00]);
        output.Write([2, 0x11]);
        output.Write([3, 0x11]);

        // spectral selection and approximation, fixed values for baseline
        output.Write([0, 63, 0]);
    }

    /// <summary>
    /// Number of bits that are needed for the magnitude of a value.
    /// </summary>
    private static int GetBitLength(int value)
    {
        var magnitude = Math.Abs(value);
        var length = 0;

        while (magnitude != 0)
        {
            magnitude >>= 1;
            length++;
        }

        return length;
    }

    /// <summary>
    /// Writes the Huffman code of a symbol, followed by the extra bits of a value.
    /// </summary>
    private static void WriteValue(JpegBitWriter writer, JpegHuffmanTable table, int symbol, int value, int bitLength)
    {
        writer.Write(table.GetCode(symbol), table.GetLength(symbol));

        if (bitLength > 0)
        {
            // Negative values are stored as the value minus one, cut to the bit length.
            var bits = value >= 0 ? value : value + (1 << bitLength) - 1;
            writer.Write(bits, bitLength);
        }
    }

    /// <summary>
    /// Converts the image to three planes: brightness (Y) and the two color channels (Cb, Cr).
    /// </summary>
    private static (double[] Y, double[] Cb, double[] Cr) ConvertToYCbCr(ReadOnlySpan<byte> bgr, int pixelCount)
    {
        var lumaPlane = new double[pixelCount];
        var cbPlane = new double[pixelCount];
        var crPlane = new double[pixelCount];

        for (var i = 0; i < pixelCount; i++)
        {
            var index = i * ChannelCount;

            double b = bgr[index];
            double g = bgr[index + 1];
            double r = bgr[index + 2];

            // JFIF color conversion (full range)
            lumaPlane[i] = (0.299 * r) + (0.587 * g) + (0.114 * b);
            cbPlane[i] = (-0.168736 * r) - (0.331264 * g) + (0.5 * b) + 128.0;
            crPlane[i] = (0.5 * r) - (0.418688 * g) - (0.081312 * b) + 128.0;
        }

        return (lumaPlane, cbPlane, crPlane);
    }

    /// <summary>
    /// Fills a block with 8x8 values of a plane. Positions outside the image use the closest pixel.
    /// </summary>
    private static void FillBlock(
        Span<double> block,
        ReadOnlySpan<double> plane,
        int width,
        int height,
        int blockX,
        int blockY
    )
    {
        for (var y = 0; y < BlockSize; y++)
        {
            var row = Math.Min(blockY + y, height - 1) * width;

            for (var x = 0; x < BlockSize; x++)
            {
                var column = Math.Min(blockX + x, width - 1);
                block[(y * BlockSize) + x] = plane[row + column] - 128.0;
            }
        }
    }

    /// <summary>
    /// Fills a block with one color channel of a 16x16 unit, averaging every 2x2 pixels to one value.
    /// Positions outside the image use the closest pixel.
    /// </summary>
    private static void FillSubsampledBlock(
        Span<double> block,
        ReadOnlySpan<double> plane,
        int width,
        int height,
        int unitX,
        int unitY
    )
    {
        for (var y = 0; y < BlockSize; y++)
        {
            var upperRow = Math.Min(unitY + (y * 2), height - 1) * width;
            var lowerRow = Math.Min(unitY + (y * 2) + 1, height - 1) * width;

            for (var x = 0; x < BlockSize; x++)
            {
                var leftColumn = Math.Min(unitX + (x * 2), width - 1);
                var rightColumn = Math.Min(unitX + (x * 2) + 1, width - 1);

                var sum = plane[upperRow + leftColumn]
                    + plane[upperRow + rightColumn]
                    + plane[lowerRow + leftColumn]
                    + plane[lowerRow + rightColumn];

                block[(y * BlockSize) + x] = (sum / 4.0) - 128.0;
            }
        }
    }

    /// <summary>
    /// Transforms, quantizes and writes one block.
    /// </summary>
    /// <returns>The DC value of the block, which the next block of the same channel is coded against.</returns>
    private static int WriteBlock(
        JpegBitWriter writer,
        Span<double> block,
        ReadOnlySpan<byte> quantization,
        JpegHuffmanTable dcTable,
        JpegHuffmanTable acTable,
        int previousDc
    )
    {
        JpegForwardDct.Transform(block);

        Span<int> coefficients = stackalloc int[BlockLength];

        for (var i = 0; i < BlockLength; i++)
        {
            var position = JpegTables.ZigZag[i];
            coefficients[i] = (int)Math.Round(block[position] / quantization[position], MidpointRounding.AwayFromZero);
        }

        // The first value (DC) is stored as the difference to the previous block.
        var dc = coefficients[0];
        var difference = dc - previousDc;
        var differenceLength = GetBitLength(difference);
        WriteValue(writer, dcTable, differenceLength, difference, differenceLength);

        // The other values (AC) are stored as pairs of "number of zeros before" and value.
        var lastNonZero = BlockLength - 1;

        while (lastNonZero > 0 && coefficients[lastNonZero] == 0)
        {
            lastNonZero--;
        }

        var zeroRun = 0;

        for (var i = 1; i <= lastNonZero; i++)
        {
            var value = coefficients[i];

            if (value == 0)
            {
                zeroRun++;
                continue;
            }

            while (zeroRun >= 16)
            {
                // symbol for "16 zeros"
                writer.Write(acTable.GetCode(0xF0), acTable.GetLength(0xF0));
                zeroRun -= 16;
            }

            var length = GetBitLength(value);
            WriteValue(writer, acTable, (zeroRun << 4) | length, value, length);
            zeroRun = 0;
        }

        if (lastNonZero < BlockLength - 1)
        {
            // symbol for "only zeros until the end of the block"
            writer.Write(acTable.GetCode(0x00), acTable.GetLength(0x00));
        }

        return dc;
    }

    private void WriteHeaders(Stream output, int width, int height)
    {
        WriteMarker(output, 0xD8); // start of image

        // JFIF header: version 1.1, 96 x 96 dots per inch, no thumbnail
        WriteMarker(output, 0xE0);
        output.Write([0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00, 0x01, 0x01, 0x01, 0x00, 0x60, 0x00, 0x60, 0x00, 0x00]);

        WriteFrameHeader(output, width, height);

        // All four Huffman tables in one segment; table class (0 = DC, 1 = AC) << 4 | table id
        var huffmanTablesLength =
            GetHuffmanTableLength(dcLuminance)
            + GetHuffmanTableLength(acLuminance)
            + GetHuffmanTableLength(dcChrominance)
            + GetHuffmanTableLength(acChrominance);

        WriteMarker(output, 0xC4);
        WriteUInt16(output, 2 + huffmanTablesLength);
        WriteHuffmanTable(output, 0x00, dcLuminance);
        WriteHuffmanTable(output, 0x10, acLuminance);
        WriteHuffmanTable(output, 0x01, dcChrominance);
        WriteHuffmanTable(output, 0x11, acChrominance);

        // Both quantization tables in one segment
        WriteMarker(output, 0xDB);
        WriteUInt16(output, 2 + (2 * (1 + BlockLength)));
        WriteQuantizationTable(output, 0, luminanceQuantization);
        WriteQuantizationTable(output, 1, chrominanceQuantization);

        WriteScanHeader(output);
    }

    /// <summary>
    /// Writes the image data. The image is split into units of 16x16 pixels; each unit is stored
    /// as four 8x8 blocks of brightness and one 8x8 block for each of the two color channels.
    /// </summary>
    private void WriteScan(Stream output, ReadOnlySpan<byte> bgr, int width, int height)
    {
        var writer = new JpegBitWriter(output);
        var (lumaPlane, cbPlane, crPlane) = ConvertToYCbCr(bgr, width * height);

        Span<double> block = stackalloc double[BlockLength];

        var previousY = 0;
        var previousCb = 0;
        var previousCr = 0;

        for (var unitY = 0; unitY < height; unitY += McuSize)
        {
            for (var unitX = 0; unitX < width; unitX += McuSize)
            {
                for (var part = 0; part < 4; part++)
                {
                    var blockX = unitX + ((part % 2) * BlockSize);
                    var blockY = unitY + ((part / 2) * BlockSize);

                    FillBlock(block, lumaPlane, width, height, blockX, blockY);
                    previousY = WriteBlock(writer, block, luminanceQuantization, dcLuminance, acLuminance, previousY);
                }

                FillSubsampledBlock(block, cbPlane, width, height, unitX, unitY);
                previousCb = WriteBlock(writer, block, chrominanceQuantization, dcChrominance, acChrominance, previousCb);

                FillSubsampledBlock(block, crPlane, width, height, unitX, unitY);
                previousCr = WriteBlock(writer, block, chrominanceQuantization, dcChrominance, acChrominance, previousCr);
            }
        }

        writer.Flush();
    }
}
