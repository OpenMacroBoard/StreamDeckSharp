using AwesomeAssertions;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace OpenMacroBoard.Tests;

public class KeyBitmapFactoryTests
{
    [Fact]
    public void RgbFactoryShouldCreateASinglePixelKeyBitmap()
    {
        byte red = 100;
        byte green = 200;
        byte blue = 0;

        var keyBitmap = KeyBitmap.Create.FromBgr24Array(1, 1, [blue, green, red]);

        var dataAccess = (IKeyBitmapDataAccess)keyBitmap;
        using var image = dataAccess.ToImage();

        image[0, 0].Should().Be(new Bgr24(red, green, blue));
    }

    [Fact]
    public void PixelFormatIsRgbLeftToRightAndTopToBottom()
    {
        var keyBitmap = KeyBitmap.Create.FromBgr24Array(2, 2,
        [
            000, 001, 002,  010, 011, 012,
            020, 021, 022,  030, 031, 032,
        ]);

        var dataAccess = (IKeyBitmapDataAccess)keyBitmap;
        using var image = dataAccess.ToImage();

        image[0, 0].Should().Be(new Bgr24(2, 1, 0));
        image[1, 0].Should().Be(new Bgr24(12, 11, 10));
        image[0, 1].Should().Be(new Bgr24(22, 21, 20));
        image[1, 1].Should().Be(new Bgr24(32, 31, 30));
    }
}
