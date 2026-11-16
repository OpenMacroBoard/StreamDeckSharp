using AwesomeAssertions;
using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Tests for the built-in image decoders. ImageSharp writes the test files and is the reference decoder.
/// </summary>
public class ImageDecoderTests
{
    public static TheoryData<PngColorType, PngBitDepth, PngInterlaceMode> PngVariants
    {
        get
        {
            var data = new TheoryData<PngColorType, PngBitDepth, PngInterlaceMode>();

            (PngColorType Color, PngBitDepth[] Depths)[] variants =
            [
                (PngColorType.Grayscale, [PngBitDepth.Bit1, PngBitDepth.Bit2, PngBitDepth.Bit4, PngBitDepth.Bit8, PngBitDepth.Bit16]),
                (PngColorType.Palette, [PngBitDepth.Bit1, PngBitDepth.Bit2, PngBitDepth.Bit4, PngBitDepth.Bit8]),
                (PngColorType.Rgb, [PngBitDepth.Bit8, PngBitDepth.Bit16]),
                (PngColorType.GrayscaleWithAlpha, [PngBitDepth.Bit8, PngBitDepth.Bit16]),
                (PngColorType.RgbWithAlpha, [PngBitDepth.Bit8, PngBitDepth.Bit16]),
            ];

            foreach (var (color, depths) in variants)
            {
                foreach (var depth in depths)
                {
                    data.Add(color, depth, PngInterlaceMode.None);
                    data.Add(color, depth, PngInterlaceMode.Adam7);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PngVariants))]
    public void PngIsDecodedLikeImageSharp(PngColorType colorType, PngBitDepth bitDepth, PngInterlaceMode interlace)
    {
        var png = Encode(new PngEncoder { ColorType = colorType, BitDepth = bitDepth, InterlaceMethod = interlace });

        AssertDecodedLikeImageSharp(png);
    }

    [Theory]
    [InlineData(1, 9, "iVBORw0KGgoAAAANSUhEUgAAAAEAAAAJBAMAAAF2b2tBAAAAG1BMVEVuJsiCLb2WNLKqO6cKA/8eCvQyEelGGN5aH9M2so/lAAAAGklEQVQImWNwYDBgaGBIYBBgCGAoYGBgUAAAFtICQeUc14kAAAAASUVORK5CYII=")]
    [InlineData(3, 2, "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACBAMAAAEYXUu/AAAAElBMVEVuJsgKA/8eCvQyEelGGN5aH9N0NLsNAAAAEUlEQVQImWMQYDBgUGBwZQAAArMApk33XkwAAAAASUVORK5CYII=")]
    [InlineData(9, 1, "iVBORw0KGgoAAAANSUhEUgAAAAkAAAABBAMAAAGJ66nYAAAAG1BMVEVuJsiCLb2WNLKqO6cKA/8eCvQyEelGGN5aH9M2so/lAAAAEUlEQVQImWNwZmhgSGQIZwIAB1UBfpxtv9oAAAAASUVORK5CYII=")]
    public void SmallInterlacedPngIsDecoded(int width, int height, string base64)
    {
        // Some of the seven interlacing passes are empty for images this small. The files were
        // written with netpbm (pnmtopng -interlace); ImageSharp writes such files incorrectly.
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

    [Theory]
    [InlineData(BmpBitsPerPixel.Pixel1)]
    [InlineData(BmpBitsPerPixel.Pixel4)]
    [InlineData(BmpBitsPerPixel.Pixel8)]
    [InlineData(BmpBitsPerPixel.Pixel16)]
    [InlineData(BmpBitsPerPixel.Pixel24)]
    [InlineData(BmpBitsPerPixel.Pixel32)]
    public void BmpIsDecodedLikeImageSharp(BmpBitsPerPixel bitsPerPixel)
    {
        var bmp = Encode(new BmpEncoder { BitsPerPixel = bitsPerPixel });

        AssertDecodedLikeImageSharp(bmp);
    }

    [Fact]
    public void BmpWithTransparencyIsDecodedLikeImageSharp()
    {
        var bmp = Encode(new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32, SupportTransparency = true });

        AssertDecodedLikeImageSharp(bmp);
    }

    [Fact]
    public async Task FileAndStreamOverloadsGiveTheSameResult()
    {
        var png = Encode(new PngEncoder());
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
        var png = Encode(new PngEncoder());

        // width and height are the first two fields of the header chunk, which follows the signature
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), 60000);
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), 60000);

        Action act = () => KeyBitmap.Create.FromStream(new MemoryStream(png));

        act.Should().Throw<ImageDecodingException>().WithMessage("*too large*");
    }

    [Fact]
    public void DamagedPngEitherDecodesOrFailsWithTheDocumentedException()
    {
        AssertDamagedDataIsHandled(Encode(new PngEncoder { InterlaceMethod = PngInterlaceMode.Adam7 }, 40, 30));
        AssertDamagedDataIsHandled(Encode(new PngEncoder { ColorType = PngColorType.Palette, BitDepth = PngBitDepth.Bit4 }, 40, 30));
    }

    [Fact]
    public void DamagedBmpEitherDecodesOrFailsWithTheDocumentedException()
    {
        AssertDamagedDataIsHandled(Encode(new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel8 }, 40, 30));
        AssertDamagedDataIsHandled(Encode(new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32, SupportTransparency = true }, 40, 30));
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

    /// <summary>
    /// The decoded pixels may differ by one from ImageSharp, because blending transparent pixels
    /// onto black can be rounded up or down.
    /// </summary>
    internal static void AssertDecodedLikeImageSharp(byte[] file, int tolerance = 1)
    {
        using var reference = Image.Load(file);
        var expected = GetData(reference.ToKeyBitmap());

        var decoded = KeyBitmap.Create.FromStream(new MemoryStream(file));

        decoded.Width.Should().Be(reference.Width);
        decoded.Height.Should().Be(reference.Height);

        GetLargestDifference(GetData(decoded), expected).Should().BeLessThanOrEqualTo(tolerance);
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

    /// <summary>
    /// Encodes the logo (which has transparent parts) in a size that is not a multiple of eight.
    /// </summary>
    private static byte[] Encode(SixLabors.ImageSharp.Formats.IImageEncoder encoder, int width = 61, int height = 47)
    {
        using var logo = AssetLoader.LoadImage("OpenMacroBoard-Logo.png");
        using var image = logo.CloneAs<Rgba32>();
        image.Mutate(x => x.Resize(width, height));

        using var stream = new MemoryStream();
        image.Save(stream, encoder);
        return stream.ToArray();
    }
}
