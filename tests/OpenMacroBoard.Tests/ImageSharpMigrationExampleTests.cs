using AwesomeAssertions;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// The code from doc/migrating-from-imagesharp.md, to make sure it compiles and works.
/// Keep both in sync.
/// </summary>
public class ImageSharpMigrationExampleTests
{
    [Fact]
    public void ImageSharpDecoderLoadsFormatsTheBuiltInDecoderDoesNotSupport()
    {
        using var image = new Image<Rgba32>(4, 3, new Rgba32(10, 200, 30));
        using var gif = new MemoryStream();
        image.SaveAsGif(gif);
        gif.Position = 0;

        var bitmap = KeyBitmap.Create.FromStream(gif, new ImageSharpDecoder());

        bitmap.Width.Should().Be(4);
        bitmap.Height.Should().Be(3);

        // blue, green, red
        ImageDecoderTests.GetData(bitmap)[..3].Should().Equal(30, 200, 10);
    }

    [Fact]
    public void ImageSharpJpegEncoderWritesAJpeg()
    {
        var pixels = new byte[16 * 16 * 3];
        pixels.AsSpan().Fill(200);

        var jpeg = new ImageSharpJpegEncoder().Encode(pixels, 16, 16);

        var decoded = OmbImageDecoder.Default.Decode(jpeg);
        decoded.Width.Should().Be(16);
        decoded.Height.Should().Be(16);
    }

    [Fact]
    public void KeyBitmapConvertsToImageSharpAndBack()
    {
        var bitmap = KeyBitmap.Create.FromBgr24Array(2, 1, [1, 2, 3, 4, 5, 6]);

        using var image = ImageSharpConversions.ToImageSharp(bitmap);
        var back = ImageSharpConversions.ToKeyBitmap(image);

        ImageDecoderTests.GetData(back).Should().Equal(1, 2, 3, 4, 5, 6);
    }

    public static class ImageSharpConversions
    {
        public static KeyBitmap ToKeyBitmap(Image image)
        {
            // Draw onto an opaque image first, so transparent pixels are blended onto black.
            using var opaque = new Image<Bgr24>(image.Width, image.Height);
            opaque.Mutate(x => x.DrawImage(image, 1));

            var pixels = new byte[opaque.Width * opaque.Height * 3];
            opaque.CopyPixelDataTo(pixels);

            return KeyBitmap.Create.FromBgr24Array(opaque.Width, opaque.Height, pixels);
        }

        public static Image<Bgr24> ToImageSharp(KeyBitmap keyBitmap)
        {
            var pixels = ((IKeyBitmapDataAccess)keyBitmap).GetData().Span;
            return Image.LoadPixelData<Bgr24>(pixels, keyBitmap.Width, keyBitmap.Height);
        }
    }

    public sealed class ImageSharpDecoder : IImageDecoder
    {
        public KeyBitmap Decode(ReadOnlySpan<byte> encodedImage)
        {
            using var image = Image.Load(encodedImage);
            return ImageSharpConversions.ToKeyBitmap(image);
        }
    }

    public sealed class ImageSharpJpegEncoder : IJpegEncoder
    {
        public byte[] Encode(ReadOnlySpan<byte> bitmapDataBgr24, int width, int height)
        {
            using var image = Image.LoadPixelData<Bgr24>(bitmapDataBgr24, width, height);
            using var stream = new MemoryStream();
            image.SaveAsJpeg(stream);
            return stream.ToArray();
        }
    }
}
