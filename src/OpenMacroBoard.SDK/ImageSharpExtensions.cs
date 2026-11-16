using OpenMacroBoard.SDK.Internals;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A collection of extensions for interop with ImageSharp.
/// </summary>
public static class ImageSharpExtensions
{
    /// <summary>
    /// Converts an Bgr24 image to a byte array containing raw pixel data.
    /// </summary>
    public static byte[] ToBgr24PixelArray(this Image<Bgr24> image)
    {
        using var ctx = image.WithBgr24();

        var data = new byte[image.Width * image.Height * 3];
        ctx.Item.CopyPixelDataTo(data);
        return data;
    }

    /// <summary>
    /// Copies an <see cref="Image"/> as Bgr24 into a given span.
    /// </summary>
    public static void ToBgr24PixelArray(this Image image, Span<byte> targetPixelData)
    {
        using var ctx = image.WithBgr24();
        ctx.Item.CopyPixelDataTo(targetPixelData);
    }

    /// <summary>
    /// Converts an <see cref="Image"/> to a byte array containing raw pixel data.
    /// </summary>
    public static byte[] ToBgr24PixelArray(this Image image)
    {
        using var ctx = image.WithBgr24();
        return ctx.Item.ToBgr24PixelArray();
    }

    /// <summary>
    /// Clones a given image into a <see cref="Image{Bgr24}"/>
    /// with correct alpha blending and a given background color.
    /// </summary>
    public static Image<Bgr24> CloneAlphaBlendedBgr24(this Image image, OmbColor backgroundColor)
    {
        var clonedBgr24 = new Image<Bgr24>(image.Width, image.Height);

        var color = Color.FromRgb(
            backgroundColor.R,
            backgroundColor.G,
            backgroundColor.B
        );

        clonedBgr24.Mutate(x =>
        {
            x.BackgroundColor(color);
            x.DrawImage(image, 1);
        });

        return clonedBgr24;
    }

    /// <summary>
    /// Clones a given image into a <see cref="Image{Bgr24}"/>
    /// with correct alpha blending and black background.
    /// </summary>
    public static Image<Bgr24> CloneAlphaBlendedBgr24(this Image image)
    {
        var clonedBgr24 = new Image<Bgr24>(image.Width, image.Height);
        clonedBgr24.Mutate(x => x.DrawImage(image, 1));
        return clonedBgr24;
    }

    /// <summary>
    /// Creates a context with an <see cref="Image{Bgr24}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the image already is an <see cref="Image{Bgr24}"/> this image will be wrapped inside the
    /// <see cref="ConditionalDisposable{T}"/>. If the image is a different pixel format it will be cloned
    /// and transformed into an <see cref="Image{Bgr24}"/>.
    /// </para>
    /// <para>
    /// Calling <see cref="IDisposable.Dispose"/> on the returned value will never dispose the original
    /// <paramref name="image"/> but only the implicitly cloned value if any.
    /// </para>
    /// </remarks>
    public static ConditionalDisposable<Image<Bgr24>> WithBgr24(this Image image)
    {
        return ConstrainedContext.For(image, x => x as Image<Bgr24>, x => x.CloneAlphaBlendedBgr24());
    }
}
