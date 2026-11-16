using AwesomeAssertions;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using Xunit;

namespace OpenMacroBoard.Tests;

public class OmbJpegEncoderTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void InvalidQualityIsRejected(int quality)
    {
        Action act = () => _ = new OmbJpegEncoder(quality);

        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("quality");
    }

    [Fact]
    public void DefaultEncoderUsesDefaultQuality()
    {
        OmbJpegEncoder.Default.Quality.Should().Be(OmbJpegEncoder.DefaultQuality);
        new OmbJpegEncoder().Quality.Should().Be(75);
    }

    [Fact]
    public void InvalidSizesAreRejected()
    {
        var encoder = new OmbJpegEncoder();

        Action zeroWidth = () => encoder.Encode(new byte[3], 0, 1);
        Action zeroHeight = () => encoder.Encode(new byte[3], 1, 0);
        Action tooWide = () => encoder.Encode(new byte[3], 65536, 1);
        Action wrongLength = () => encoder.Encode(new byte[11], 2, 2);

        zeroWidth.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("width");
        zeroHeight.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("height");
        tooWide.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("width");
        wrongLength.Should().Throw<ArgumentException>().And.ParamName.Should().Be("bitmapDataBgr24");
    }

    [Fact]
    public void OutputIsAJpegFile()
    {
        var jpeg = new OmbJpegEncoder().Encode(CreateGradient(16, 16), 16, 16);

        // starts with "start of image" and the JFIF header, ends with "end of image"
        jpeg[..4].Should().Equal(0xFF, 0xD8, 0xFF, 0xE0);
        jpeg[6..10].Should().Equal((byte)'J', (byte)'F', (byte)'I', (byte)'F');
        jpeg[^2..].Should().Equal(0xFF, 0xD9);
    }

    [Fact]
    public void EncodingIsDeterministic()
    {
        var pixels = CreateGradient(72, 72);
        var encoder = new OmbJpegEncoder();

        encoder.Encode(pixels, 72, 72).Should().Equal(encoder.Encode(pixels, 72, 72));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 5)]
    [InlineData(8, 8)]
    [InlineData(16, 16)]
    [InlineData(17, 33)]
    [InlineData(72, 72)]
    [InlineData(96, 96)]
    [InlineData(120, 120)]
    [InlineData(200, 100)]
    public void GradientSurvivesEncodingAsWellAsWithImageSharp(int width, int height)
    {
        var pixels = CreateGradient(width, height);

        var decoded = Decode(new OmbJpegEncoder().Encode(pixels, width, height), width, height);
        var error = KeyBitmapTransformTests.MeanAbsoluteDifference(decoded, pixels);

        // Small images with steep gradients lose a lot in any JPEG encoder, so the
        // reference is what ImageSharp's encoder loses with the same settings.
        var referenceError = GetImageSharpError(pixels, width, height, OmbJpegEncoder.DefaultQuality);

        error.Should().BeLessThan((referenceError * 1.25) + 1.0);
    }

    [Theory]
    [InlineData(0x00, 0x00, 0x00)]
    [InlineData(0xFF, 0xFF, 0xFF)]
    [InlineData(0xFF, 0x00, 0x00)]
    [InlineData(0x00, 0xFF, 0x00)]
    [InlineData(0x00, 0x00, 0xFF)]
    [InlineData(0x6A, 0x4A, 0x4F)]
    public void SingleColorSurvivesEncoding(byte r, byte g, byte b)
    {
        const int size = 72;
        var pixels = new byte[size * size * 3];

        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = b;
            pixels[i + 1] = g;
            pixels[i + 2] = r;
        }

        var decoded = Decode(new OmbJpegEncoder().Encode(pixels, size, size), size, size);

        for (var i = 0; i < decoded.Length; i++)
        {
            Math.Abs(decoded[i] - pixels[i]).Should().BeLessThanOrEqualTo(2);
        }
    }

    [Fact]
    public void LogoSurvivesEncoding()
    {
        using var logo = AssetLoader.LoadImage("OpenMacroBoard-Logo.png");
        using var image = logo.CloneAs<Bgr24>();

        var pixels = new byte[image.Width * image.Height * 3];
        image.CopyPixelDataTo(pixels);

        var decoded = Decode(new OmbJpegEncoder(90).Encode(pixels, image.Width, image.Height), image.Width, image.Height);
        var error = KeyBitmapTransformTests.MeanAbsoluteDifference(decoded, pixels);

        error.Should().BeLessThan((GetImageSharpError(pixels, image.Width, image.Height, 90) * 1.25) + 1.0);
    }

    [Fact]
    public void HigherQualityIsCloserToTheOriginalAndLarger()
    {
        const int size = 96;

        // noise is hard to compress, so the quality setting makes a clear difference
        var pixels = new byte[size * size * 3];
        new Random(7).NextBytes(pixels);

        var low = new OmbJpegEncoder(20).Encode(pixels, size, size);
        var high = new OmbJpegEncoder(95).Encode(pixels, size, size);

        high.Length.Should().BeGreaterThan(low.Length);

        var lowError = KeyBitmapTransformTests.MeanAbsoluteDifference(Decode(low, size, size), pixels);
        var highError = KeyBitmapTransformTests.MeanAbsoluteDifference(Decode(high, size, size), pixels);

        highError.Should().BeLessThan(lowError);
    }

    /// <summary>
    /// Decodes with ImageSharp, as an independent check that the output is a valid JPEG.
    /// </summary>
    private static byte[] Decode(byte[] jpeg, int expectedWidth, int expectedHeight)
    {
        using var image = Image.Load<Bgr24>(jpeg);

        image.Width.Should().Be(expectedWidth);
        image.Height.Should().Be(expectedHeight);

        var result = new byte[image.Width * image.Height * 3];
        image.CopyPixelDataTo(result);
        return result;
    }

    /// <summary>
    /// Encodes and decodes with ImageSharp and returns how far the result is from the original.
    /// </summary>
    private static double GetImageSharpError(byte[] pixels, int width, int height, int quality)
    {
        using var image = Image.LoadPixelData<Bgr24>(pixels, width, height);
        using var stream = new MemoryStream();

        image.SaveAsJpeg(stream, new JpegEncoder { Quality = quality });

        return KeyBitmapTransformTests.MeanAbsoluteDifference(Decode(stream.ToArray(), width, height), pixels);
    }

    private static byte[] CreateGradient(int width, int height)
    {
        var pixels = new byte[width * height * 3];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = ((y * width) + x) * 3;

                pixels[index] = (byte)(255 * x / Math.Max(1, width - 1));
                pixels[index + 1] = (byte)(255 * y / Math.Max(1, height - 1));
                pixels[index + 2] = (byte)(255 - (255 * (x + y) / Math.Max(1, width + height - 2)));
            }
        }

        return pixels;
    }
}
