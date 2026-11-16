# Migrating from the ImageSharp based API

Up to version 10 `OpenMacroBoard.SDK` and `StreamDeckSharp` used [ImageSharp](https://github.com/SixLabors/ImageSharp)
and exposed some of its types. Since version 11 they have no dependency on it. Scaling, rotating,
JPEG encoding and decoding of BMP, PNG and JPEG files are built in.

This page shows what replaces the removed API. If you still want to use ImageSharp in your own
project, for example to draw text, reference it yourself and use the small helpers below.

## What was removed, and what to use instead

| Removed | Use instead |
| --- | --- |
| `KeyBitmap.Create.FromImageSharpImage(image)` | `ImageSharpConversions.ToKeyBitmap(image)` (helper below) |
| `keyBitmap.ToImage()` | `ImageSharpConversions.ToImageSharp(keyBitmap)` (helper below) |
| `board.DrawFullScreenBitmap(image, ResizeMode.X)` | `board.DrawFullScreenBitmap(keyBitmap, OmbResizeMode.X)` |
| `ImageSharpExtensions` (`ToBgr24PixelArray`, `CloneAlphaBlendedBgr24`, `WithBgr24`) | ImageSharp's own `CopyPixelDataTo`, see the helper below |
| `ConditionalDisposable<T>` | no replacement, it was only used by the methods above |

`OmbResizeMode` has the modes `BoxPad` (the default, as before), `Pad`, `Crop` and `Stretch`.
For ImageSharp's other modes (`Max`, `Min`, `Manual`) resize the image with ImageSharp first.

`KeyBitmap.Create.FromFile` and `FromStream` still exist. They now read BMP, PNG and JPEG files
with the built-in decoder and throw `ImageDecodingException` for anything else. To load other
formats, pass a decoder as shown below.

## Converting between ImageSharp images and key bitmaps

```csharp
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
```

## Loading other image formats with ImageSharp

```csharp
public sealed class ImageSharpDecoder : IImageDecoder
{
    public KeyBitmap Decode(ReadOnlySpan<byte> encodedImage)
    {
        using var image = Image.Load(encodedImage);
        return ImageSharpConversions.ToKeyBitmap(image);
    }
}

var bitmap = KeyBitmap.Create.FromFile("animation.gif", new ImageSharpDecoder());
```

## Encoding JPEG with ImageSharp

Stream Decks that expect JPEG images use the built-in `OmbJpegEncoder` by default. To use
ImageSharp's encoder instead:

```csharp
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

var options = new StreamDeckOptions { JpegEncoder = new ImageSharpJpegEncoder() };
using var board = StreamDeck.OpenDevice(options);
```

The same two interfaces work with any other image library.
