using System.Drawing;
using System.Drawing.Imaging;
using FluentAssertions;
using OpenMacroBoard.SDK;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class KeyBitmapFactoryTests
    {
        [Fact]
        public void RgbFactoryShouldCreateASinglePixelKeyBitmap()
        {
            byte red = 100;
            byte green = 200;
            byte blue = 0;

            var expectation = new KeyBitmap(1, 1, new byte[] { blue, green, red });
            var key = KeyBitmap.Create.FromRgb(red, green, blue);

            key.Should().Be(expectation);
        }

        [Fact]
        public void RgbFactoryShouldCreateANullDataElementForBlack()
        {
            var expectation = new KeyBitmap(1, 1, null);
            var wrongResult = new KeyBitmap(1, 1, new byte[] { 0, 0, 0 });

            var key = KeyBitmap.Create.FromRgb(0, 0, 0);
            key.Should().Be(expectation);
            key.Should().NotBe(wrongResult);
        }

        [Fact]
        public void PixelFormatIsRgbLeftToRightAndTopToBottom()
        {
            var expectation = new KeyBitmap(2, 2, new byte[2 * 2 * 3]
            {
                000, 001, 002,  010, 011, 012,
                020, 021, 022,  030, 031, 032,
            });

            var topLeft = Color.FromArgb(2, 1, 0);
            var topRight = Color.FromArgb(12, 11, 10);
            var bottomLeft = Color.FromArgb(22, 21, 20);
            var bottomRight = Color.FromArgb(32, 31, 30);

            var img = new Bitmap(2, 2, PixelFormat.Format24bppRgb);
            img.SetPixel(0, 0, topLeft);
            img.SetPixel(1, 0, topRight);
            img.SetPixel(0, 1, bottomLeft);
            img.SetPixel(1, 1, bottomRight);

            var key = KeyBitmap.Create.FromBitmap(img);
            key.Should().Be(expectation);
        }
    }
}
