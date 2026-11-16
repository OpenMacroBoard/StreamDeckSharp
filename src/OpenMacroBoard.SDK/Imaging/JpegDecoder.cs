using System;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Decodes JPEG files: baseline and progressive, grayscale and color (YCbCr or RGB),
/// with any chroma subsampling and with restart markers.
/// </summary>
/// <remarks>
/// <para>Not supported: CMYK images, lossless and arithmetic coded files, and more than 8 bits per
/// sample. Color profiles and the EXIF orientation are ignored.</para>
/// <para>Color channels that are stored with a lower resolution are enlarged by repeating pixels.</para>
/// </remarks>
internal sealed class JpegDecoder
{
    private const int BlockSize = 8;
    private const int BlockLength = BlockSize * BlockSize;
    private const int TableCount = 4;
    private const int MaxSamplingFactor = 4;

    private const int MarkerStartOfImage = 0xD8;
    private const int MarkerEndOfImage = 0xD9;
    private const int MarkerStartOfScan = 0xDA;
    private const int MarkerQuantization = 0xDB;
    private const int MarkerHuffman = 0xC4;
    private const int MarkerRestartInterval = 0xDD;
    private const int MarkerAdobe = 0xEE;
    private const int MarkerFirstRestart = 0xD0;
    private const int MarkerLastRestart = 0xD7;

    private readonly byte[] data;
    private readonly int[]?[] quantization = new int[TableCount][];
    private readonly JpegHuffmanDecodeTable?[] dcTables = new JpegHuffmanDecodeTable[TableCount];
    private readonly JpegHuffmanDecodeTable?[] acTables = new JpegHuffmanDecodeTable[TableCount];

    private int position;
    private int width;
    private int height;
    private bool progressive;
    private Component[] components = [];
    private int maxHorizontal;
    private int maxVertical;
    private int restartInterval;
    private int adobeTransform = -1;
    private bool anyScanDecoded;

    private int bitBuffer;
    private int bitCount;
    private bool markerReached;

    private int spectralStart;
    private int spectralEnd;
    private int approximationHigh;
    private int approximationLow;
    private int endOfBandRun;

    private JpegDecoder(byte[] data)
    {
        this.data = data;
    }

    public static bool IsJpeg(ReadOnlySpan<byte> data)
    {
        return data.StartsWith<byte>([0xFF, MarkerStartOfImage, 0xFF]);
    }

    public static DecodedImage Decode(ReadOnlySpan<byte> data)
    {
        var decoder = new JpegDecoder(data.ToArray());

        decoder.ReadSegments();
        return decoder.CreateImage();
    }

    private static int Clamp(double value)
    {
        return Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
    }

    /// <summary>
    /// Turns the bits of a value into the value: values with a leading zero bit are negative.
    /// </summary>
    private static int Extend(int bits, int length)
    {
        return bits < (1 << (length - 1)) ? bits - (1 << length) + 1 : bits;
    }

    /// <summary>
    /// The inverse discrete cosine transform for one 8x8 block, in place.
    /// </summary>
    private static void InverseTransform(Span<double> block)
    {
        Span<double> line = stackalloc double[BlockSize];
        Span<double> result = stackalloc double[BlockSize];

        for (var y = 0; y < BlockSize; y++)
        {
            var row = block.Slice(y * BlockSize, BlockSize);
            InverseTransform1D(row, result);
            result.CopyTo(row);
        }

        for (var x = 0; x < BlockSize; x++)
        {
            for (var y = 0; y < BlockSize; y++)
            {
                line[y] = block[(y * BlockSize) + x];
            }

            InverseTransform1D(line, result);

            for (var y = 0; y < BlockSize; y++)
            {
                block[(y * BlockSize) + x] = result[y];
            }
        }
    }

    private static void InverseTransform1D(ReadOnlySpan<double> input, Span<double> output)
    {
        var cosines = JpegForwardDct.CosineTable;

        for (var n = 0; n < BlockSize; n++)
        {
            var sum = input[0] * JpegForwardDct.FirstCoefficientScale;

            for (var u = 1; u < BlockSize; u++)
            {
                sum += input[u] * cosines[(u * BlockSize) + n];
            }

            output[n] = 0.5 * sum;
        }
    }

    private static void SetPixel(DecodedImage image, int x, int y, ReadOnlySpan<int> samples, bool isRgb)
    {
        const byte opaque = 255;

        if (samples.Length == 1)
        {
            var gray = (byte)samples[0];
            image.SetPixel(x, y, gray, gray, gray, opaque);
        }
        else if (isRgb)
        {
            image.SetPixel(x, y, (byte)samples[0], (byte)samples[1], (byte)samples[2], opaque);
        }
        else
        {
            // JFIF color conversion (full range)
            double luma = samples[0];
            double cb = samples[1] - 128;
            double cr = samples[2] - 128;

            image.SetPixel(
                x,
                y,
                (byte)Clamp(luma + (1.402 * cr)),
                (byte)Clamp(luma - (0.344136 * cb) - (0.714136 * cr)),
                (byte)Clamp(luma + (1.772 * cb)),
                opaque
            );
        }
    }

    private int ReadByte()
    {
        return data[position++];
    }

    private int ReadUInt16()
    {
        var value = (data[position] << 8) | data[position + 1];
        position += 2;
        return value;
    }

    /// <summary>
    /// Reads the file segment by segment until the end of the image.
    /// </summary>
    private void ReadSegments()
    {
        // skip the start of image marker
        position = 2;

        while (position + 1 < data.Length)
        {
            if (data[position] != 0xFF)
            {
                // not at a marker (damaged file or unknown data), look for the next one
                position++;
                continue;
            }

            int marker = data[position + 1];

            if (marker is 0xFF or 0x00)
            {
                position++;
                continue;
            }

            position += 2;

            if (marker == MarkerEndOfImage)
            {
                return;
            }

            if (marker is (>= MarkerFirstRestart and <= MarkerLastRestart) or MarkerStartOfImage)
            {
                // markers without content
                continue;
            }

            // the length includes its own two bytes
            var segmentStart = position;
            var segmentEnd = segmentStart + ReadUInt16();
            ReadSegment(marker, segmentEnd);

            if (marker != MarkerStartOfScan)
            {
                position = segmentEnd;
            }
        }
    }

    private void ReadSegment(int marker, int segmentEnd)
    {
        switch (marker)
        {
            case MarkerQuantization:
                ReadQuantizationTables(segmentEnd);
                break;

            case MarkerHuffman:
                ReadHuffmanTables(segmentEnd);
                break;

            case MarkerRestartInterval:
                restartInterval = ReadUInt16();
                break;

            case MarkerAdobe:
                ReadAdobeSegment(segmentEnd);
                break;

            case 0xC0 or 0xC1 or 0xC2:
                progressive = marker == 0xC2;
                ReadFrameHeader();
                break;

            case 0xC3 or (>= 0xC5 and <= 0xCF and not 0xC8 and not 0xCC):
                throw new ImageDecodingException("This kind of JPEG file (lossless or arithmetic coded) is not supported.");

            case MarkerStartOfScan:
                ReadScan();
                break;

            default:
                // application data, comments and anything else: not needed
                break;
        }
    }

    private void ReadQuantizationTables(int segmentEnd)
    {
        while (position < segmentEnd)
        {
            var info = ReadByte();
            var is16Bit = (info >> 4) != 0;
            var table = new int[BlockLength];

            // The values are stored in zigzag order; the table is kept in block order.
            for (var i = 0; i < BlockLength; i++)
            {
                table[JpegTables.ZigZag[i]] = is16Bit ? ReadUInt16() : ReadByte();
            }

            quantization[info & 0x0F] = table;
        }
    }

    private void ReadHuffmanTables(int segmentEnd)
    {
        const int codeLengthCount = 16;

        while (position < segmentEnd)
        {
            var info = ReadByte();
            var counts = data.AsSpan(position, codeLengthCount);
            position += codeLengthCount;

            var symbolCount = 0;

            foreach (var count in counts)
            {
                symbolCount += count;
            }

            var table = new JpegHuffmanDecodeTable(counts, data.AsSpan(position, symbolCount));
            position += symbolCount;

            var tables = (info >> 4) == 0 ? dcTables : acTables;
            tables[info & 0x0F] = table;
        }
    }

    /// <summary>
    /// Reads the segment that Adobe software writes. It says whether a color image is stored as RGB.
    /// </summary>
    private void ReadAdobeSegment(int segmentEnd)
    {
        const int transformOffset = 11;

        if (segmentEnd - position > transformOffset && data.AsSpan(position, 5).SequenceEqual("Adobe"u8))
        {
            adobeTransform = data[position + transformOffset];
        }
    }

    private void ReadFrameHeader()
    {
        if (components.Length != 0)
        {
            throw new ImageDecodingException("JPEG files with more than one image are not supported.");
        }

        var precision = ReadByte();
        height = ReadUInt16();
        width = ReadUInt16();
        var componentCount = ReadByte();

        if (precision != 8)
        {
            throw new ImageDecodingException("Only JPEG files with 8 bits per sample are supported.");
        }

        if (componentCount is not (1 or 3))
        {
            throw new ImageDecodingException("Only grayscale and color JPEG files are supported (no CMYK).");
        }

        DecodedImage.EnsureValidSize(width, height);

        var result = new Component[componentCount];

        for (var i = 0; i < componentCount; i++)
        {
            var id = ReadByte();
            var sampling = ReadByte();
            var quantizationId = ReadByte();

            var horizontal = sampling >> 4;
            var vertical = sampling & 0x0F;

            if (horizontal is < 1 or > MaxSamplingFactor || vertical is < 1 or > MaxSamplingFactor || quantizationId >= TableCount)
            {
                throw new ImageDecodingException("The JPEG frame header is invalid.");
            }

            result[i] = new Component(id, horizontal, vertical, quantizationId);
            maxHorizontal = Math.Max(maxHorizontal, horizontal);
            maxVertical = Math.Max(maxVertical, vertical);
        }

        // The image is stored in units that contain every component at least once.
        var unitsPerLine = (width + (BlockSize * maxHorizontal) - 1) / (BlockSize * maxHorizontal);
        var unitsPerColumn = (height + (BlockSize * maxVertical) - 1) / (BlockSize * maxVertical);

        foreach (var component in result)
        {
            component.BlocksPerLine = unitsPerLine * component.Horizontal;
            component.BlocksPerColumn = unitsPerColumn * component.Vertical;
            component.Coefficients = new short[checked(component.BlocksPerLine * component.BlocksPerColumn * BlockLength)];

            // The size of the component alone, for scans that contain only this component.
            var componentWidth = ((width * component.Horizontal) + maxHorizontal - 1) / maxHorizontal;
            var componentHeight = ((height * component.Vertical) + maxVertical - 1) / maxVertical;

            component.ScanBlocksPerLine = (componentWidth + BlockSize - 1) / BlockSize;
            component.ScanBlocksPerColumn = (componentHeight + BlockSize - 1) / BlockSize;
        }

        components = result;
    }

    /// <summary>
    /// Reads a scan: its header, followed by the compressed coefficients.
    /// </summary>
    /// <exception cref="ImageDecodingException">The data of the JPEG file is not valid.</exception>
    private void ReadScan()
    {
        if (components.Length == 0)
        {
            throw new ImageDecodingException("The JPEG file has image data before its frame header.");
        }

        var count = ReadByte();

        if (count < 1 || count > components.Length)
        {
            throw new ImageDecodingException("The JPEG scan header is invalid.");
        }

        var scanComponents = new Component[count];

        for (var i = 0; i < count; i++)
        {
            var id = ReadByte();
            var tables = ReadByte();

            var component = Array.Find(components, c => c.Id == id)
                ?? throw new ImageDecodingException("The JPEG scan header is invalid.");

            component.DcTable = dcTables[(tables >> 4) & 0x03];
            component.AcTable = acTables[tables & 0x03];
            component.PreviousDc = 0;
            scanComponents[i] = component;
        }

        spectralStart = ReadByte();
        spectralEnd = ReadByte();
        var approximation = ReadByte();
        approximationHigh = approximation >> 4;
        approximationLow = approximation & 0x0F;

        if (!progressive)
        {
            spectralStart = 0;
            spectralEnd = BlockLength - 1;
            approximationHigh = 0;
            approximationLow = 0;
        }

        if (spectralStart > spectralEnd || spectralEnd >= BlockLength || (spectralStart > 0 && count != 1))
        {
            throw new ImageDecodingException("The JPEG scan header is invalid.");
        }

        bitCount = 0;
        markerReached = false;
        endOfBandRun = 0;

        if (count == 1)
        {
            // A scan with a single component stores only the blocks that component needs.
            var component = scanComponents[0];
            var total = component.ScanBlocksPerLine * component.ScanBlocksPerColumn;

            for (var i = 0; i < total; i++)
            {
                DecodeBlock(component, i % component.ScanBlocksPerLine, i / component.ScanBlocksPerLine);
                HandleRestart(scanComponents, i + 1, total);
            }
        }
        else
        {
            var unitsPerLine = components[0].BlocksPerLine / components[0].Horizontal;
            var unitsPerColumn = components[0].BlocksPerColumn / components[0].Vertical;
            var total = unitsPerLine * unitsPerColumn;

            for (var i = 0; i < total; i++)
            {
                var unitX = i % unitsPerLine;
                var unitY = i / unitsPerLine;

                foreach (var component in scanComponents)
                {
                    for (var v = 0; v < component.Vertical; v++)
                    {
                        for (var h = 0; h < component.Horizontal; h++)
                        {
                            DecodeBlock(component, (unitX * component.Horizontal) + h, (unitY * component.Vertical) + v);
                        }
                    }
                }

                HandleRestart(scanComponents, i + 1, total);
            }
        }

        anyScanDecoded = true;
    }

    /// <summary>
    /// After every restart interval the data continues at a restart marker with a fresh state.
    /// </summary>
    private void HandleRestart(Component[] scanComponents, int decodedUnits, int totalUnits)
    {
        if (restartInterval == 0 || decodedUnits % restartInterval != 0 || decodedUnits == totalUnits)
        {
            return;
        }

        // skip the rest of the current byte and look for the marker
        bitCount = 0;

        while (position + 1 < data.Length && !(data[position] == 0xFF && data[position + 1] is not (0x00 or 0xFF)))
        {
            position++;
        }

        if (position + 1 < data.Length && data[position + 1] is >= MarkerFirstRestart and <= MarkerLastRestart)
        {
            position += 2;
            markerReached = false;
        }

        endOfBandRun = 0;

        foreach (var component in scanComponents)
        {
            component.PreviousDc = 0;
        }
    }

    /// <summary>
    /// Reads the next bit of the compressed data. After the end of the data only zeros are returned.
    /// </summary>
    private int ReadBit()
    {
        if (bitCount == 0)
        {
            bitBuffer = 0;
            bitCount = BlockSize;

            if (!markerReached && position < data.Length)
            {
                int value = data[position];

                if (value != 0xFF)
                {
                    bitBuffer = value;
                    position++;
                }
                else if (position + 1 < data.Length && data[position + 1] == 0)
                {
                    // 0xFF followed by a zero byte is a data byte 0xFF
                    bitBuffer = value;
                    position += 2;
                }
                else
                {
                    // a marker: the compressed data ends here
                    markerReached = true;
                }
            }
        }

        bitCount--;
        return (bitBuffer >> bitCount) & 1;
    }

    private int ReadBits(int length)
    {
        var value = 0;

        for (var i = 0; i < length; i++)
        {
            value = (value << 1) | ReadBit();
        }

        return value;
    }

    private int ReadSymbol(JpegHuffmanDecodeTable? table)
    {
        const int maxCodeLength = 16;

        if (table is null)
        {
            throw new ImageDecodingException("The JPEG file uses a Huffman table that it does not define.");
        }

        var code = 0;

        for (var length = 1; length <= maxCodeLength; length++)
        {
            code = (code << 1) | ReadBit();
            var symbol = table.Find(code, length);

            if (symbol >= 0)
            {
                return symbol;
            }
        }

        throw new ImageDecodingException("The image data of the JPEG file is damaged.");
    }

    private void DecodeBlock(Component component, int blockX, int blockY)
    {
        var block = component.Coefficients.AsSpan(((blockY * component.BlocksPerLine) + blockX) * BlockLength, BlockLength);

        if (!progressive)
        {
            DecodeSequentialBlock(component, block);
        }
        else if (spectralStart == 0)
        {
            DecodeProgressiveDc(component, block);
        }
        else if (approximationHigh == 0)
        {
            DecodeProgressiveAcFirst(component, block);
        }
        else
        {
            DecodeProgressiveAcRefine(component, block);
        }
    }

    /// <summary>
    /// Reads the difference of the first coefficient (DC) to the one of the previous block.
    /// </summary>
    private int ReadDcDifference(Component component)
    {
        var length = ReadSymbol(component.DcTable);
        return length == 0 ? 0 : Extend(ReadBits(length), length);
    }

    private void DecodeSequentialBlock(Component component, Span<short> block)
    {
        component.PreviousDc += ReadDcDifference(component);
        block[0] = (short)component.PreviousDc;

        var index = 1;

        while (index < BlockLength)
        {
            var symbol = ReadSymbol(component.AcTable);
            var zeroRun = symbol >> 4;
            var length = symbol & 0x0F;

            if (length == 0)
            {
                if (zeroRun != 15)
                {
                    // only zeros until the end of the block
                    break;
                }

                index += 16;
                continue;
            }

            index += zeroRun;
            block[JpegTables.ZigZag[index]] = (short)Extend(ReadBits(length), length);
            index++;
        }
    }

    /// <summary>
    /// Progressive files store the first coefficient in a first scan (the upper bits)
    /// and further scans that each add one more bit.
    /// </summary>
    private void DecodeProgressiveDc(Component component, Span<short> block)
    {
        if (approximationHigh == 0)
        {
            component.PreviousDc += ReadDcDifference(component);
            block[0] = (short)(component.PreviousDc << approximationLow);
        }
        else if (ReadBit() == 1)
        {
            block[0] |= (short)(1 << approximationLow);
        }
    }

    /// <summary>
    /// First scan of a range of the other coefficients (AC): the upper bits of the values.
    /// </summary>
    private void DecodeProgressiveAcFirst(Component component, Span<short> block)
    {
        if (endOfBandRun > 0)
        {
            // this block is part of a run of blocks without further values
            endOfBandRun--;
            return;
        }

        var index = spectralStart;

        while (index <= spectralEnd)
        {
            var symbol = ReadSymbol(component.AcTable);
            var zeroRun = symbol >> 4;
            var length = symbol & 0x0F;

            if (length == 0)
            {
                if (zeroRun != 15)
                {
                    // a run of blocks without further values, this block included
                    endOfBandRun = (1 << zeroRun) - 1;

                    if (zeroRun > 0)
                    {
                        endOfBandRun += ReadBits(zeroRun);
                    }

                    break;
                }

                index += 16;
                continue;
            }

            index += zeroRun;
            block[JpegTables.ZigZag[index]] = (short)(Extend(ReadBits(length), length) * (1 << approximationLow));
            index++;
        }
    }

    /// <summary>
    /// Later scan of a range of the other coefficients (AC): one more bit for values that are
    /// already known, and new values that only have this bit.
    /// </summary>
    private void DecodeProgressiveAcRefine(Component component, Span<short> block)
    {
        var positiveBit = 1 << approximationLow;
        var negativeBit = -1 << approximationLow;
        var index = spectralStart;

        if (endOfBandRun == 0)
        {
            while (index <= spectralEnd)
            {
                var symbol = ReadSymbol(component.AcTable);
                var zeroRun = symbol >> 4;
                var newValue = 0;

                if ((symbol & 0x0F) != 0)
                {
                    // a new value: it can only be plus or minus this bit
                    newValue = ReadBit() == 1 ? positiveBit : negativeBit;
                }
                else if (zeroRun != 15)
                {
                    // a run of blocks without new values, this block included
                    endOfBandRun = 1 << zeroRun;

                    if (zeroRun > 0)
                    {
                        endOfBandRun += ReadBits(zeroRun);
                    }

                    break;
                }

                // Skip the given number of positions that are still zero. Values that are already
                // known lie in between; each of them gets its next bit on the way.
                while (index <= spectralEnd)
                {
                    var blockPosition = JpegTables.ZigZag[index];

                    if (block[blockPosition] != 0)
                    {
                        RefineValue(block, blockPosition, positiveBit, negativeBit);
                    }
                    else
                    {
                        zeroRun--;

                        if (zeroRun < 0)
                        {
                            break;
                        }
                    }

                    index++;
                }

                if (newValue != 0 && index <= spectralEnd)
                {
                    block[JpegTables.ZigZag[index]] = (short)newValue;
                }

                index++;
            }
        }

        if (endOfBandRun > 0)
        {
            // No new values in this block, but the known ones still get their next bit.
            while (index <= spectralEnd)
            {
                var blockPosition = JpegTables.ZigZag[index];

                if (block[blockPosition] != 0)
                {
                    RefineValue(block, blockPosition, positiveBit, negativeBit);
                }

                index++;
            }

            endOfBandRun--;
        }
    }

    /// <summary>
    /// Reads one bit and, if it is set, adds it to a value that is already known.
    /// </summary>
    private void RefineValue(Span<short> block, int blockPosition, int positiveBit, int negativeBit)
    {
        if (ReadBit() == 1 && (block[blockPosition] & positiveBit) == 0)
        {
            block[blockPosition] += (short)(block[blockPosition] >= 0 ? positiveBit : negativeBit);
        }
    }

    /// <summary>
    /// Turns the coefficients of all components into pixels.
    /// </summary>
    /// <exception cref="ImageDecodingException">The data of the JPEG file is not valid.</exception>
    private DecodedImage CreateImage()
    {
        if (!anyScanDecoded)
        {
            throw new ImageDecodingException("The JPEG file contains no image data.");
        }

        var image = new DecodedImage(width, height);
        var planes = new byte[components.Length][];

        for (var i = 0; i < components.Length; i++)
        {
            planes[i] = CreatePlane(components[i]);
        }

        // Without a hint from Adobe software a color image is YCbCr.
        var isRgb = adobeTransform == 0;
        Span<int> samples = stackalloc int[components.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var i = 0; i < components.Length; i++)
                {
                    var component = components[i];

                    // Components with a lower resolution repeat their pixels.
                    var sourceX = x * component.Horizontal / maxHorizontal;
                    var sourceY = y * component.Vertical / maxVertical;

                    samples[i] = planes[i][(sourceY * component.BlocksPerLine * BlockSize) + sourceX];
                }

                SetPixel(image, x, y, samples, isRgb);
            }
        }

        return image;
    }

    /// <summary>
    /// Creates the pixels of one component: every block is multiplied with the quantization table
    /// and transformed back.
    /// </summary>
    /// <exception cref="ImageDecodingException">The data of the JPEG file is not valid.</exception>
    private byte[] CreatePlane(Component component)
    {
        var table = quantization[component.QuantizationId]
            ?? throw new ImageDecodingException("The JPEG file uses a quantization table that it does not define.");

        var lineLength = component.BlocksPerLine * BlockSize;
        var plane = new byte[checked(lineLength * component.BlocksPerColumn * BlockSize)];
        Span<double> block = stackalloc double[BlockLength];

        for (var blockY = 0; blockY < component.BlocksPerColumn; blockY++)
        {
            for (var blockX = 0; blockX < component.BlocksPerLine; blockX++)
            {
                var coefficients = component.Coefficients.AsSpan(((blockY * component.BlocksPerLine) + blockX) * BlockLength, BlockLength);

                if (coefficients[1..].IndexOfAnyExcept((short)0) < 0)
                {
                    // Only the first coefficient is set, which is common: the block has a single value.
                    // The calculation is the one the transform would do, so the result is the same.
                    var scale = JpegForwardDct.FirstCoefficientScale;
                    block.Fill(0.5 * (0.5 * (coefficients[0] * table[0] * scale) * scale));
                }
                else
                {
                    for (var i = 0; i < BlockLength; i++)
                    {
                        block[i] = coefficients[i] * table[i];
                    }

                    InverseTransform(block);
                }

                for (var y = 0; y < BlockSize; y++)
                {
                    var target = (((blockY * BlockSize) + y) * lineLength) + (blockX * BlockSize);

                    for (var x = 0; x < BlockSize; x++)
                    {
                        plane[target + x] = (byte)Clamp(block[(y * BlockSize) + x] + 128.0);
                    }
                }
            }
        }

        return plane;
    }

    /// <summary>
    /// One channel of the image: brightness or one of the two color channels.
    /// </summary>
    private sealed class Component
    {
        public Component(int id, int horizontal, int vertical, int quantizationId)
        {
            Id = id;
            Horizontal = horizontal;
            Vertical = vertical;
            QuantizationId = quantizationId;
        }

        public int Id { get; }

        /// <summary>
        /// Gets the horizontal sampling factor: how many blocks per unit this component has in a line.
        /// </summary>
        public int Horizontal { get; }

        /// <summary>
        /// Gets the vertical sampling factor.
        /// </summary>
        public int Vertical { get; }

        public int QuantizationId { get; }

        public int BlocksPerLine { get; set; }

        public int BlocksPerColumn { get; set; }

        public int ScanBlocksPerLine { get; set; }

        public int ScanBlocksPerColumn { get; set; }

        public short[] Coefficients { get; set; } = [];

        public JpegHuffmanDecodeTable? DcTable { get; set; }

        public JpegHuffmanDecodeTable? AcTable { get; set; }

        public int PreviousDc { get; set; }
    }
}
