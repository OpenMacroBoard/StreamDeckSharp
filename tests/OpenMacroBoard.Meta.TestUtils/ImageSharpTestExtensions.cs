using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace OpenMacroBoard.Meta.TestUtils;

/// <summary>
/// Conversions between <see cref="KeyBitmap"/> and ImageSharp images, for tests only.
/// The tests use ImageSharp to draw test images and as a reference to compare against.
/// </summary>
public static class ImageSharpTestExtensions
{
    /// <summary>
    /// Creates an ImageSharp image with the pixels of a key bitmap.
    /// </summary>
    public static Image<Bgr24> ToImage(this IKeyBitmapDataAccess dataAccess)
    {
        ArgumentNullException.ThrowIfNull(dataAccess);

        return Image.LoadPixelData<Bgr24>(
            dataAccess.GetData().ToArray(),
            dataAccess.Width,
            dataAccess.Height
        );
    }

    /// <summary>
    /// Creates a key bitmap with the pixels of an ImageSharp image. Transparent pixels are blended onto black.
    /// </summary>
    public static KeyBitmap ToKeyBitmap(this Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        using var opaque = new Image<Bgr24>(image.Width, image.Height);
        opaque.Mutate(x => x.DrawImage(image, 1));

        var pixels = new byte[image.Width * image.Height * 3];
        opaque.CopyPixelDataTo(pixels);

        return KeyBitmap.Create.FromBgr24Array(image.Width, image.Height, pixels);
    }
}
