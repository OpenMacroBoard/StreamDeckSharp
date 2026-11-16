using AwesomeAssertions;
using OpenMacroBoard.SDK;
using Xunit;

namespace OpenMacroBoard.Tests;

public class KeyBitmapFactoryTests
{
    [Fact]
    public void RgbFactoryShouldCreateASinglePixelKeyBitmap()
    {
        var keyBitmap = KeyBitmap.Create.FromRgb(100, 200, 0);

        keyBitmap.Width.Should().Be(1);
        keyBitmap.Height.Should().Be(1);

        // stored as blue, green, red
        ImageDecoderTests.GetData(keyBitmap).Should().Equal(0, 200, 100);
    }

    [Fact]
    public void PixelFormatIsBgrLeftToRightAndTopToBottom()
    {
        byte[] pixels =
        [
            000, 001, 002,  010, 011, 012,
            020, 021, 022,  030, 031, 032,
        ];

        var keyBitmap = KeyBitmap.Create.FromBgr24Array(2, 2, pixels);

        keyBitmap.Width.Should().Be(2);
        keyBitmap.Height.Should().Be(2);
        ImageDecoderTests.GetData(keyBitmap).Should().Equal(pixels);
    }
}
