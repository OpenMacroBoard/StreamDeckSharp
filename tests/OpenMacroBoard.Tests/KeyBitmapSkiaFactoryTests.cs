using AwesomeAssertions;
using OpenMacroBoard.SDK;
using SkiaSharp;
using System;
using Xunit;

namespace OpenMacroBoard.Tests;

public class KeyBitmapSkiaFactoryTests
{
    [Fact]
    public void CanvasStartsBlackAndDrawnPixelsArrive()
    {
        using var paint = new SKPaint { Color = new SKColor(200, 100, 50), IsAntialias = false };

        // 5 x 3 pixels: an odd width, so a row is not a multiple of anything convenient.
        var key = KeyBitmap.Create.FromSkiaCanvas(5, 3, canvas => canvas.DrawRect(1, 1, 2, 1, paint));

        key.Width.Should().Be(5);
        key.Height.Should().Be(3);

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                var drawn = y == 1 && x is 1 or 2;
                GetRgb(key, x, y).Should().Be(drawn ? (200, 100, 50) : (0, 0, 0), "pixel {0},{1}", x, y);
            }
        }
    }

    [Fact]
    public void TransparentPartsOfTheCanvasBecomeBlack()
    {
        using var half = new SKPaint { Color = new SKColor(255, 255, 255, 128), BlendMode = SKBlendMode.Src };

        var key = KeyBitmap.Create.FromSkiaCanvas(2, 1, canvas =>
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawRect(1, 0, 1, 1, half);
        });

        GetRgb(key, 0, 0).Should().Be((0, 0, 0));
        GetRgb(key, 1, 0).Should().Be((128, 128, 128));
    }

    [Theory]
    [InlineData(SKColorType.Rgba8888, SKAlphaType.Unpremul)]
    [InlineData(SKColorType.Rgba8888, SKAlphaType.Premul)]
    [InlineData(SKColorType.Bgra8888, SKAlphaType.Unpremul)]
    [InlineData(SKColorType.Rgb565, SKAlphaType.Opaque)]
    public void BitmapsOfDifferentColorTypesAreConverted(SKColorType colorType, SKAlphaType alphaType)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(3, 2, colorType, alphaType));

        bitmap.Erase(SKColors.Black);
        bitmap.SetPixel(0, 0, new SKColor(255, 0, 0));
        bitmap.SetPixel(2, 0, new SKColor(0, 255, 0));
        bitmap.SetPixel(1, 1, new SKColor(0, 0, 255));

        var key = KeyBitmap.Create.FromSkiaBitmap(bitmap);

        key.Width.Should().Be(3);
        key.Height.Should().Be(2);

        GetRgb(key, 0, 0).Should().Be((255, 0, 0));
        GetRgb(key, 1, 0).Should().Be((0, 0, 0));
        GetRgb(key, 2, 0).Should().Be((0, 255, 0));
        GetRgb(key, 1, 1).Should().Be((0, 0, 255));
    }

    [Fact]
    public void TransparentPartsOfABitmapAreBlendedOntoBlack()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));

        bitmap.SetPixel(0, 0, new SKColor(255, 255, 255, 0));
        bitmap.SetPixel(1, 0, new SKColor(200, 100, 50, 128));

        var key = KeyBitmap.Create.FromSkiaBitmap(bitmap);
        var (r, g, b) = GetRgb(key, 1, 0);

        GetRgb(key, 0, 0).Should().Be((0, 0, 0));

        // 128 of 255 is a little more than half
        r.Should().BeInRange(99, 101);
        g.Should().BeInRange(49, 51);
        b.Should().BeInRange(24, 26);
    }

    [Fact]
    public void InvalidArgumentsAreRejected()
    {
        Action noAction = () => KeyBitmap.Create.FromSkiaCanvas(10, 10, null);
        Action noWidth = () => KeyBitmap.Create.FromSkiaCanvas(0, 10, _ => { });
        Action noHeight = () => KeyBitmap.Create.FromSkiaCanvas(10, 0, _ => { });
        Action noBitmap = () => KeyBitmap.Create.FromSkiaBitmap(null);

        Action emptyBitmap = () =>
        {
            using var empty = new SKBitmap();
            KeyBitmap.Create.FromSkiaBitmap(empty);
        };

        noAction.Should().Throw<ArgumentNullException>();
        noWidth.Should().Throw<ArgumentOutOfRangeException>();
        noHeight.Should().Throw<ArgumentOutOfRangeException>();
        noBitmap.Should().Throw<ArgumentNullException>();
        emptyBitmap.Should().Throw<ArgumentException>();
    }

    private static (int R, int G, int B) GetRgb(KeyBitmap key, int x, int y)
    {
        var data = ((IKeyBitmapDataAccess)key).GetData().Span;
        var index = ((y * key.Width) + x) * 3;

        return (data[index + 2], data[index + 1], data[index]);
    }
}
