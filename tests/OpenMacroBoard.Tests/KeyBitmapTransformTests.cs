using AwesomeAssertions;
using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;
using Xunit;

namespace OpenMacroBoard.Tests;

public class KeyBitmapTransformTests
{
    [Fact]
    public void Rotate90TurnsClockwise()
    {
        // 0 1        4 2 0
        // 2 3   =>   5 3 1
        // 4 5
        var result = CreateNumbered(2, 3).Rotate90();

        result.Width.Should().Be(3);
        result.Height.Should().Be(2);
        GetNumbers(result).Should().Equal(4, 2, 0, 5, 3, 1);
    }

    [Fact]
    public void Rotate180TurnsUpsideDown()
    {
        var result = CreateNumbered(2, 3).Rotate180();

        result.Width.Should().Be(2);
        result.Height.Should().Be(3);
        GetNumbers(result).Should().Equal(5, 4, 3, 2, 1, 0);
    }

    [Fact]
    public void Rotate270TurnsCounterclockwise()
    {
        // 0 1        1 3 5
        // 2 3   =>   0 2 4
        // 4 5
        var result = CreateNumbered(2, 3).Rotate270();

        result.Width.Should().Be(3);
        result.Height.Should().Be(2);
        GetNumbers(result).Should().Equal(1, 3, 5, 0, 2, 4);
    }

    [Fact]
    public void FlipHorizontalSwapsLeftAndRight()
    {
        var result = CreateNumbered(2, 3).FlipHorizontal();

        result.Width.Should().Be(2);
        result.Height.Should().Be(3);
        GetNumbers(result).Should().Equal(1, 0, 3, 2, 5, 4);
    }

    [Fact]
    public void FlipVerticalSwapsTopAndBottom()
    {
        var result = CreateNumbered(2, 3).FlipVertical();

        result.Width.Should().Be(2);
        result.Height.Should().Be(3);
        GetNumbers(result).Should().Equal(4, 5, 2, 3, 0, 1);
    }

    [Fact]
    public void RotateAndFlipMatchImageSharp()
    {
        var source = CreateRandom(13, 7, 1);

        GetData(source.Rotate90()).Should().Equal(WithImageSharp(source, x => x.Rotate(RotateMode.Rotate90)));
        GetData(source.Rotate180()).Should().Equal(WithImageSharp(source, x => x.Rotate(RotateMode.Rotate180)));
        GetData(source.Rotate270()).Should().Equal(WithImageSharp(source, x => x.Rotate(RotateMode.Rotate270)));
        GetData(source.FlipHorizontal()).Should().Equal(WithImageSharp(source, x => x.Flip(FlipMode.Horizontal)));
        GetData(source.FlipVertical()).Should().Equal(WithImageSharp(source, x => x.Flip(FlipMode.Vertical)));

        GetData(source.Rotate270().FlipVertical())
            .Should().Equal(WithImageSharp(source, x => x.RotateFlip(RotateMode.Rotate270, FlipMode.Vertical)));
    }

    [Fact]
    public void ResizeToSameSizeReturnsTheSameBitmap()
    {
        var source = CreateRandom(8, 6, 2);

        source.Resize(8, 6).Should().BeSameAs(source);
    }

    [Fact]
    public void ResizeRejectsInvalidSizes()
    {
        var source = CreateRandom(8, 6, 3);

        Action zeroWidth = () => source.Resize(0, 6);
        Action negativeHeight = () => source.Resize(8, -1);

        zeroWidth.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("width");
        negativeHeight.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("height");
    }

    [Theory]
    [InlineData(1, 1, 72, 72)]
    [InlineData(5, 3, 96, 96)]
    [InlineData(200, 100, 72, 72)]
    [InlineData(72, 72, 1, 1)]
    public void ResizeKeepsASingleColorExactly(int sourceWidth, int sourceHeight, int width, int height)
    {
        var pixels = new byte[sourceWidth * sourceHeight * 3];

        for (var i = 0; i < pixels.Length; i += 3)
        {
            pixels[i] = 0x4F;
            pixels[i + 1] = 0x4A;
            pixels[i + 2] = 0x6A;
        }

        var result = KeyBitmap.Create.FromBgr24Array(sourceWidth, sourceHeight, pixels).Resize(width, height);

        result.Width.Should().Be(width);
        result.Height.Should().Be(height);

        var data = GetData(result);
        data.Should().HaveCount(width * height * 3);

        for (var i = 0; i < data.Length; i += 3)
        {
            data[i].Should().Be(0x4F);
            data[i + 1].Should().Be(0x4A);
            data[i + 2].Should().Be(0x6A);
        }
    }

    [Theory]
    [InlineData(72, 72)]
    [InlineData(96, 96)]
    [InlineData(300, 120)]
    [InlineData(37, 91)]
    public void ResizeIsCloseToImageSharp(int width, int height)
    {
        using var logo = AssetLoader.LoadImage("OpenMacroBoard-Logo.png");
        using var reference = logo.CloneAs<Bgr24>();
        var source = reference.ToKeyBitmap();

        var expected = WithImageSharp(source, x => x.Resize(width, height, KnownResamplers.Triangle));
        var actual = GetData(source.Resize(width, height));

        actual.Should().HaveCount(expected.Length);
        MeanAbsoluteDifference(actual, expected).Should().BeLessThan(2.0);
    }

    internal static double MeanAbsoluteDifference(byte[] left, byte[] right)
    {
        long sum = 0;

        for (var i = 0; i < left.Length; i++)
        {
            sum += Math.Abs(left[i] - right[i]);
        }

        return (double)sum / left.Length;
    }

    internal static byte[] GetData(KeyBitmap bitmap)
    {
        return ((IKeyBitmapDataAccess)bitmap).GetData().ToArray();
    }

    internal static KeyBitmap CreateRandom(int width, int height, int seed)
    {
        var pixels = new byte[width * height * 3];
        new Random(seed).NextBytes(pixels);
        return KeyBitmap.Create.FromBgr24Array(width, height, pixels);
    }

    /// <summary>
    /// Creates a bitmap where every pixel has its own number (row by row), stored in the blue channel.
    /// </summary>
    private static KeyBitmap CreateNumbered(int width, int height)
    {
        var pixels = new byte[width * height * 3];

        for (var i = 0; i < width * height; i++)
        {
            pixels[i * 3] = (byte)i;
            pixels[(i * 3) + 1] = (byte)(i + 100);
            pixels[(i * 3) + 2] = (byte)(i + 200);
        }

        return KeyBitmap.Create.FromBgr24Array(width, height, pixels);
    }

    private static int[] GetNumbers(KeyBitmap bitmap)
    {
        var data = GetData(bitmap);

        // green and red must have moved together with blue
        for (var i = 0; i < data.Length; i += 3)
        {
            data[i + 1].Should().Be((byte)(data[i] + 100));
            data[i + 2].Should().Be((byte)(data[i] + 200));
        }

        return [.. Enumerable.Range(0, data.Length / 3).Select(i => (int)data[i * 3])];
    }

    private static byte[] WithImageSharp(KeyBitmap bitmap, Action<IImageProcessingContext> operation)
    {
        using var image = Image.LoadPixelData<Bgr24>(GetData(bitmap), bitmap.Width, bitmap.Height);
        image.Mutate(operation);

        var result = new byte[image.Width * image.Height * 3];
        image.CopyPixelDataTo(result);
        return result;
    }
}
