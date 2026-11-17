using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OpenMacroBoard.Examples.CommonStuff;

/// <summary>
/// Converts between ImageSharp images and key bitmaps.
/// </summary>
/// <remarks>
/// <para>
/// The libraries do not depend on ImageSharp. The examples use it for drawing, and this is
/// all that is needed to show an ImageSharp image on a board. The same code is in
/// doc/migrating-from-imagesharp.md.
/// </para>
/// </remarks>
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
