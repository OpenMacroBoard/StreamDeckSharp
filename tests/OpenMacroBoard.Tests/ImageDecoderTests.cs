using AwesomeAssertions;
using OpenMacroBoard.SDK;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Tests for the built-in PNG and BMP decoders. The test files in Assets/Imaging were written with
/// ImageMagick from the logo (33 x 21 pixels, with transparency). Next to every file is a reference
/// with the pixels ImageMagick decodes from it, as raw red, green, blue, alpha bytes.
/// </summary>
public class ImageDecoderTests
{
    private const int AssetWidth = 33;
    private const int AssetHeight = 21;

    public static TheoryData<string> PngFiles
    {
        get
        {
            var data = new TheoryData<string>();

            string[] names =
            [
                "png_gray_1", "png_gray_2", "png_gray_4", "png_gray_8", "png_gray_16",
                "png_palette_1", "png_palette_2", "png_palette_4", "png_palette_8", "png_palette_alpha",
                "png_rgb_8", "png_rgb_16",
                "png_grayalpha_8", "png_grayalpha_16",
                "png_rgba_8", "png_rgba_16",
            ];

            foreach (var name in names)
            {
                data.Add($"{name}.png");
                data.Add($"{name}_interlaced.png");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PngFiles))]
    public void PngIsDecodedLikeImageMagick(string file)
    {
        AssertDecodedLikeReference(file);
    }

    [Theory]
    [InlineData("bmp_1.bmp")]
    [InlineData("bmp_4.bmp")]
    [InlineData("bmp_8.bmp")]
    [InlineData("bmp_16_555.bmp")]
    [InlineData("bmp_16_565.bmp")]
    [InlineData("bmp_24.bmp")]
    [InlineData("bmp_32_alpha.bmp")]
    [InlineData("bmp_os2_24.bmp")]
    public void BmpIsDecodedLikeImageMagick(string file)
    {
        AssertDecodedLikeReference(file);
    }

    [Fact]
    public void CompressedBmpIsRejected()
    {
        // run-length encoded, 8 bits per pixel
        var bmp = AssetLoader.LoadBytes("Imaging.unsupported_rle8.bmp");

        Action act = () => OmbImageDecoder.Default.Decode(bmp);

        act.Should().Throw<ImageDecodingException>().WithMessage("*Compressed BMP*");
    }

    [Fact]
    public void BmpWithRowsFromTopToBottomIsDecoded()
    {
        // A negative height means that the rows are stored from top to bottom. The test file stores
        // them from bottom to top, so the rows are put in reverse order and the height is negated.
        var bmp = AssetLoader.LoadBytes("Imaging.bmp_24.bmp");
        var expected = GetData(OmbImageDecoder.Default.Decode(bmp));

        var pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10));
        var rowLength = ((AssetWidth * 3) + 3) / 4 * 4;
        var topDown = (byte[])bmp.Clone();

        for (var y = 0; y < AssetHeight; y++)
        {
            bmp.AsSpan(pixelOffset + (y * rowLength), rowLength)
                .CopyTo(topDown.AsSpan(pixelOffset + ((AssetHeight - 1 - y) * rowLength)));
        }

        BinaryPrimitives.WriteInt32LittleEndian(topDown.AsSpan(22), -AssetHeight);

        GetData(OmbImageDecoder.Default.Decode(topDown)).Should().Equal(expected);
    }

    [Theory]
    [InlineData(1, 9, "iVBORw0KGgoAAAANSUhEUgAAAAEAAAAJBAMAAAF2b2tBAAAAG1BMVEVuJsiCLb2WNLKqO6cKA/8eCvQyEelGGN5aH9M2so/lAAAAGklEQVQImWNwYDBgaGBIYBBgCGAoYGBgUAAAFtICQeUc14kAAAAASUVORK5CYII=")]
    [InlineData(3, 2, "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACBAMAAAEYXUu/AAAAElBMVEVuJsgKA/8eCvQyEelGGN5aH9N0NLsNAAAAEUlEQVQImWMQYDBgUGBwZQAAArMApk33XkwAAAAASUVORK5CYII=")]
    [InlineData(9, 1, "iVBORw0KGgoAAAANSUhEUgAAAAkAAAABBAMAAAGJ66nYAAAAG1BMVEVuJsiCLb2WNLKqO6cKA/8eCvQyEelGGN5aH9M2so/lAAAAEUlEQVQImWNwZmhgSGQIZwIAB1UBfpxtv9oAAAAASUVORK5CYII=")]
    public void SmallInterlacedPngIsDecoded(int width, int height, string base64)
    {
        // Some of the seven interlacing passes are empty for images this small.
        // The files were written with netpbm (pnmtopng -interlace).
        // Pixel number i (row by row) has the color (i * 20 + 10, i * 7 + 3, 255 - i * 11).
        var decoded = KeyBitmap.Create.FromStream(new MemoryStream(Convert.FromBase64String(base64)));

        decoded.Width.Should().Be(width);
        decoded.Height.Should().Be(height);

        var data = GetData(decoded);

        for (var i = 0; i < width * height; i++)
        {
            data[(i * 3) + 2].Should().Be((byte)((i * 20) + 10));
            data[(i * 3) + 1].Should().Be((byte)((i * 7) + 3));
            data[i * 3].Should().Be((byte)(255 - (i * 11)));
        }
    }

    [Fact]
    public void TransparentPixelsAreBlendedOntoBlack()
    {
        // red with 50% opacity on black is half as bright
        byte[] bgra = [0, 0, 255, 128];

        var bitmap = KeyBitmap.Create.FromBgra32Array(1, 1, bgra);

        // FromBgra32Array ignores alpha; decoding a file with transparency does not
        GetData(bitmap).Should().Equal(0, 0, 255);

        var decoded = GetData(OmbImageDecoder.Default.Decode(AssetLoader.LoadBytes("Imaging.png_rgba_8.png")));
        var reference = AssetLoader.LoadBytes("Imaging.png_rgba_8.rgba");

        // the logo has fully transparent pixels in its corners, they must be black
        reference[3].Should().Be(0);
        decoded[..3].Should().Equal(0, 0, 0);
    }

    [Fact]
    public async Task FileAndStreamOverloadsGiveTheSameResult()
    {
        var png = AssetLoader.LoadBytes("Imaging.png_rgba_8.png");
        var expected = GetData(KeyBitmap.Create.FromStream(new MemoryStream(png)));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");

        try
        {
            await File.WriteAllBytesAsync(path, png, TestContext.Current.CancellationToken);

            GetData(KeyBitmap.Create.FromFile(path)).Should().Equal(expected);
            GetData(await KeyBitmap.Create.FromFileAsync(path)).Should().Equal(expected);
            GetData(await KeyBitmap.Create.FromStreamAsync(new MemoryStream(png))).Should().Equal(expected);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PngThatClaimsAHugeSizeIsRejected()
    {
        var png = AssetLoader.LoadBytes("Imaging.png_rgba_8.png");

        // width and height are the first two fields of the header chunk, which follows the signature
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), 60000);
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), 60000);

        Action act = () => KeyBitmap.Create.FromStream(new MemoryStream(png));

        act.Should().Throw<ImageDecodingException>().WithMessage("*too large*");
    }

    [Theory]
    [InlineData("png_rgba_8_interlaced.png")]
    [InlineData("png_palette_4.png")]
    [InlineData("png_gray_16.png")]
    [InlineData("bmp_8.bmp")]
    [InlineData("bmp_32_alpha.bmp")]
    [InlineData("bmp_16_565.bmp")]
    public void DamagedFileEitherDecodesOrFailsWithTheDocumentedException(string file)
    {
        AssertDamagedDataIsHandled(AssetLoader.LoadBytes($"Imaging.{file}"));
    }

    /// <summary>
    /// Cuts the file off at many lengths and changes bytes at random positions. The decoder must
    /// never fail with anything but <see cref="ImageDecodingException"/>.
    /// </summary>
    internal static void AssertDamagedDataIsHandled(byte[] original)
    {
        // The first bytes identify the format; without them the file is not ours to decode.
        const int signatureLength = 8;

        for (var length = signatureLength; length < original.Length; length += Math.Max(1, original.Length / 150))
        {
            TryDecode(original[..length]);
        }

        var random = new Random(1234);

        for (var i = 0; i < 400; i++)
        {
            var damaged = (byte[])original.Clone();
            var count = random.Next(1, 4);

            for (var j = 0; j < count; j++)
            {
                damaged[random.Next(signatureLength, damaged.Length)] = (byte)random.Next(256);
            }

            TryDecode(damaged);
        }
    }

    internal static byte[] GetData(KeyBitmap bitmap)
    {
        return ((IKeyBitmapDataAccess)bitmap).GetData().ToArray();
    }

    internal static int GetLargestDifference(byte[] actual, byte[] expected)
    {
        actual.Should().HaveCount(expected.Length);

        var largestDifference = 0;

        for (var i = 0; i < actual.Length; i++)
        {
            largestDifference = Math.Max(largestDifference, Math.Abs(actual[i] - expected[i]));
        }

        return largestDifference;
    }

    /// <summary>
    /// Compares the decoded file with its reference. The reference has an alpha channel;
    /// the decoder blends transparent pixels onto black, so the reference is blended the same way.
    /// </summary>
    private static void AssertDecodedLikeReference(string file)
    {
        var decoded = OmbImageDecoder.Default.Decode(AssetLoader.LoadBytes($"Imaging.{file}"));
        var reference = AssetLoader.LoadBytes($"Imaging.{Path.GetFileNameWithoutExtension(file)}.rgba");

        decoded.Width.Should().Be(AssetWidth);
        decoded.Height.Should().Be(AssetHeight);
        reference.Should().HaveCount(AssetWidth * AssetHeight * 4);

        var expected = new byte[AssetWidth * AssetHeight * 3];

        for (var i = 0; i < AssetWidth * AssetHeight; i++)
        {
            int alpha = reference[(i * 4) + 3];

            // reference is red, green, blue; the bitmap is blue, green, red
            expected[(i * 3) + 2] = Blend(reference[i * 4], alpha);
            expected[(i * 3) + 1] = Blend(reference[(i * 4) + 1], alpha);
            expected[i * 3] = Blend(reference[(i * 4) + 2], alpha);
        }

        // Files with 16 bits per sample (or 5 and 6 bits in 16 bit BMP files) have to be reduced
        // or extended to 8 bits. The result can differ by one from ImageMagick, which does not
        // always round to the nearest value.
        var tolerance = file.Contains("16", StringComparison.Ordinal) ? 1 : 0;

        GetLargestDifference(GetData(decoded), expected).Should().BeLessThanOrEqualTo(tolerance);
    }

    private static byte Blend(int value, int alpha)
    {
        return (byte)(((value * alpha) + 127) / 255);
    }

    private static void TryDecode(byte[] data)
    {
        try
        {
            _ = KeyBitmap.Create.FromStream(new MemoryStream(data));
        }
        catch (ImageDecodingException)
        {
            // expected for damaged files
        }
    }
}
