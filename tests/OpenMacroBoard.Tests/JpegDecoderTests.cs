using AwesomeAssertions;
using OpenMacroBoard.SDK;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Tests for the built-in JPEG decoder. The test files were written with libjpeg (cjpeg), and
/// every file has a reference image next to it that libjpeg decoded (djpeg -nosmooth -dct float).
/// </summary>
public class JpegDecoderTests
{
    [Theory]
    [InlineData("base_420")]
    [InlineData("base_444")]
    [InlineData("base_422")]
    [InlineData("base_440")]
    [InlineData("base_411")]
    [InlineData("base_gray")]
    [InlineData("base_rgb")]
    [InlineData("base_restart")]
    [InlineData("base_restart_blocks")]
    [InlineData("base_optimized")]
    [InlineData("base_q10")]
    [InlineData("base_q100")]
    [InlineData("prog_420")]
    [InlineData("prog_444")]
    [InlineData("prog_422")]
    [InlineData("prog_gray")]
    [InlineData("prog_restart")]
    [InlineData("prog_q10")]
    public void JpegIsDecodedLikeLibjpeg(string name)
    {
        var decoded = KeyBitmap.Create.FromStream(new MemoryStream(AssetLoader.LoadBytes($"Jpeg.{name}.jpg")));
        var (width, height, expected) = LoadReference(name);

        decoded.Width.Should().Be(width);
        decoded.Height.Should().Be(height);

        var actual = ImageDecoderTests.GetData(decoded);
        var (largest, _) = Compare(actual, expected);

        // Most files decode to exactly the same pixels; rounding can differ by one.
        largest.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void ArithmeticCodedJpegIsRejected()
    {
        var jpeg = AssetLoader.LoadBytes("Jpeg.unsupported_arithmetic.jpg");

        Action act = () => KeyBitmap.Create.FromStream(new MemoryStream(jpeg));

        act.Should().Throw<ImageDecodingException>().WithMessage("*not supported*");
    }

    [Fact]
    public void UnknownFormatIsRejected()
    {
        // the first bytes of a GIF file
        var gif = Encoding.ASCII.GetBytes("GIF89a\u0001\u0000\u0001\u0000");

        Action act = () => KeyBitmap.Create.FromStream(new MemoryStream(gif));
        Action empty = () => KeyBitmap.Create.FromStream(new MemoryStream());

        act.Should().Throw<ImageDecodingException>().WithMessage("*Unknown image format*");
        empty.Should().Throw<ImageDecodingException>();
    }

    [Theory]
    [InlineData("base_420")]
    [InlineData("base_restart")]
    [InlineData("prog_420")]
    [InlineData("prog_restart")]
    public void DamagedJpegEitherDecodesOrFailsWithTheDocumentedException(string name)
    {
        ImageDecoderTests.AssertDamagedDataIsHandled(AssetLoader.LoadBytes($"Jpeg.{name}.jpg"));
    }

    [Fact]
    public void LargePhotoIsDecodedCloseToImageSharp()
    {
        // 3840 x 2160 pixels. ImageSharp enlarges the color channels differently,
        // so the pixels are close but not identical.
        var jpeg = AssetLoader.LoadBytes("ExampleImageText.jpg");

        var decoded = KeyBitmap.Create.FromStream(new MemoryStream(jpeg));

        using var reference = SixLabors.ImageSharp.Image.Load(jpeg);
        var expected = ImageDecoderTests.GetData(KeyBitmap.Create.FromImageSharpImage(reference));

        decoded.Width.Should().Be(reference.Width);
        decoded.Height.Should().Be(reference.Height);

        var (largest, mean) = Compare(ImageDecoderTests.GetData(decoded), expected);
        largest.Should().BeLessThanOrEqualTo(4);
        mean.Should().BeLessThan(0.5);
    }

    private static (int Largest, double Mean) Compare(byte[] actual, byte[] expected)
    {
        actual.Should().HaveCount(expected.Length);

        var largest = 0;
        long sum = 0;

        for (var i = 0; i < actual.Length; i++)
        {
            var difference = Math.Abs(actual[i] - expected[i]);
            largest = Math.Max(largest, difference);
            sum += difference;
        }

        return (largest, (double)sum / actual.Length);
    }

    /// <summary>
    /// Loads a reference image (binary PGM or PPM file) as BGR24.
    /// </summary>
    private static (int Width, int Height, byte[] Bgr) LoadReference(string name)
    {
        var file = AssetLoader.LoadBytes($"Jpeg.{name}.ppm");

        // header: "P5" (gray) or "P6" (color), width, height and maximum value, separated by whitespace
        var position = 0;

        string ReadToken()
        {
            var start = position;

            while (!char.IsWhiteSpace((char)file[position]))
            {
                position++;
            }

            var token = Encoding.ASCII.GetString(file, start, position - start);
            position++;
            return token;
        }

        var channels = ReadToken() == "P5" ? 1 : 3;
        var width = int.Parse(ReadToken(), CultureInfo.InvariantCulture);
        var height = int.Parse(ReadToken(), CultureInfo.InvariantCulture);
        ReadToken();

        var bgr = new byte[width * height * 3];

        for (var i = 0; i < width * height; i++)
        {
            var source = position + (i * channels);

            bgr[(i * 3) + 2] = file[source];
            bgr[(i * 3) + 1] = file[source + (channels == 1 ? 0 : 1)];
            bgr[i * 3] = file[source + (channels == 1 ? 0 : 2)];
        }

        return (width, height, bgr);
    }
}
