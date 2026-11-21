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

    /// <summary>
    /// The same 48 x 32 image with every EXIF orientation (written with exiftool). The reference
    /// images come from ImageMagick ("convert -auto-orient"), which turns them for display.
    /// </summary>
    [Theory]
    [InlineData("orient_1", 48, 32)]
    [InlineData("orient_2", 48, 32)]
    [InlineData("orient_3", 48, 32)]
    [InlineData("orient_4", 48, 32)]
    [InlineData("orient_5", 32, 48)]
    [InlineData("orient_6", 32, 48)]
    [InlineData("orient_7", 32, 48)]
    [InlineData("orient_8", 32, 48)]
    [InlineData("orient_6_le", 32, 48)]
    public void JpegIsTurnedLikeItsExifOrientationSays(string name, int expectedWidth, int expectedHeight)
    {
        var decoded = KeyBitmap.Create.FromStream(new MemoryStream(AssetLoader.LoadBytes($"Jpeg.{name}.jpg")));
        var (width, height, expected) = LoadReference(name);

        width.Should().Be(expectedWidth);
        height.Should().Be(expectedHeight);
        decoded.Width.Should().Be(width);
        decoded.Height.Should().Be(height);

        var (largest, _) = Compare(ImageDecoderTests.GetData(decoded), expected);

        // ImageMagick decodes with other settings of libjpeg than the reference images above, so
        // values can differ a little more. A wrong orientation differs by more than 100.
        largest.Should().BeLessThanOrEqualTo(3);
    }

    [Theory]
    [InlineData("orient_6")]
    [InlineData("orient_6_le")]
    public void JpegWithDamagedExifDataEitherDecodesOrFailsWithTheDocumentedException(string name)
    {
        ImageDecoderTests.AssertDamagedDataIsHandled(AssetLoader.LoadBytes($"Jpeg.{name}.jpg"));
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
    public void LargePhotoIsDecodedLikeLibjpeg()
    {
        // 3840 x 2160 pixels. A reference image of that size would be too large for the repository,
        // so a few pixels are compared with what libjpeg decodes (red, green, blue).
        (int X, int Y, byte R, byte G, byte B)[] expected =
        [
            (0, 0, 121, 107, 72),
            (3839, 0, 167, 160, 141),
            (0, 2159, 81, 101, 125),
            (3839, 2159, 60, 60, 60),
            (1326, 617, 8, 15, 33),
            (1617, 197, 73, 89, 167),
            (296, 385, 48, 65, 83),
            (1497, 237, 13, 20, 38),
            (3726, 2078, 68, 68, 68),
            (879, 153, 221, 219, 204),
            (352, 1776, 42, 57, 88),
            (1712, 286, 6, 13, 31),
            (985, 371, 121, 129, 142),
            (2257, 1738, 208, 194, 209),
            (242, 507, 38, 64, 91),
            (914, 253, 165, 164, 170),
        ];

        var decoded = AssetLoader.LoadBitmap("ExampleImageText.jpg");

        decoded.Width.Should().Be(3840);
        decoded.Height.Should().Be(2160);

        var data = ((IKeyBitmapDataAccess)decoded).GetData().Span;

        foreach (var (x, y, r, g, b) in expected)
        {
            var index = ((y * decoded.Width) + x) * 3;

            // libjpeg converts the colors with integer tables, so its rounding can differ a little
            Math.Abs(data[index + 2] - r).Should().BeLessThanOrEqualTo(2);
            Math.Abs(data[index + 1] - g).Should().BeLessThanOrEqualTo(2);
            Math.Abs(data[index] - b).Should().BeLessThanOrEqualTo(2);
        }
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
