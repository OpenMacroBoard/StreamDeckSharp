using SkiaSharp;
using System;

#pragma warning disable AV1505 // Namespace should match with assembly name

namespace OpenMacroBoard.SDK;

/// <summary>
/// Collection of factory extension methods for <see cref="KeyBitmap"/>s that use SkiaSharp.
/// </summary>
/// <remarks>
/// <para>You typically don't want to invoke the static methods directly, but instead use them
/// as extension methods by calling <see cref="KeyBitmap.Create"/>.XYZ();</para>
/// </remarks>
public static class KeyBitmapSkiaFactoryExtensions
{
    private const int BytesPerPixel = 4;

    /// <summary>
    /// Creates a key bitmap from drawing commands on a SkiaSharp canvas (for example with a lambda expression).
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="width">width of the bitmap</param>
    /// <param name="height">height of the bitmap</param>
    /// <param name="drawAction">action that draws on the <see cref="SKCanvas"/>. The canvas starts black.</param>
    /// <returns>The drawn image. Parts that are transparent in the end are black.</returns>
    /// <exception cref="ArgumentNullException">
    /// Is thrown if <paramref name="drawAction"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either <paramref name="width"/> or <paramref name="height"/> are smaller than one.
    /// </exception>
    public static KeyBitmap FromSkiaCanvas(
        this IKeyBitmapFactory keyFactory,
        int width,
        int height,
        Action<SKCanvas> drawAction
    )
    {
        ArgumentNullException.ThrowIfNull(drawAction);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        using var target = CreateTarget(width, height);

        using (var canvas = new SKCanvas(target))
        {
            canvas.Clear(SKColors.Black);
            drawAction(canvas);
        }

        return ToKeyBitmap(keyFactory, target);
    }

    /// <summary>
    /// Creates a <see cref="KeyBitmap"/> from a given <see cref="SKBitmap"/> of any color type.
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="bitmap">The bitmap used to create the <see cref="KeyBitmap"/></param>
    /// <returns>A copy of the image. Transparent parts are blended onto black.</returns>
    /// <exception cref="ArgumentNullException">
    /// Is thrown if <paramref name="bitmap"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Is thrown if <paramref name="bitmap"/> has no pixels.
    /// </exception>
    public static KeyBitmap FromSkiaBitmap(this IKeyBitmapFactory keyFactory, SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (bitmap.Width < 1 || bitmap.Height < 1)
        {
            throw new ArgumentException("The bitmap has no pixels.", nameof(bitmap));
        }

        // Drawing onto a black image converts every color type and blends transparent parts onto black.
        using var target = CreateTarget(bitmap.Width, bitmap.Height);

        using (var canvas = new SKCanvas(target))
        {
            canvas.Clear(SKColors.Black);
            canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);
        }

        return ToKeyBitmap(keyFactory, target);
    }

    private static SKBitmap CreateTarget(int width, int height)
    {
        return new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
    }

    private static KeyBitmap ToKeyBitmap(IKeyBitmapFactory keyFactory, SKBitmap target)
    {
        var width = target.Width;
        var height = target.Height;
        var rowLength = width * BytesPerPixel;

        ReadOnlySpan<byte> source = target.GetPixelSpan();
        var pixels = new byte[rowLength * height];

        // Rows are copied one by one, a row of the bitmap can be longer than its pixels.
        for (var y = 0; y < height; y++)
        {
            source.Slice(y * target.RowBytes, rowLength).CopyTo(pixels.AsSpan(y * rowLength, rowLength));
        }

        // The colors are premultiplied with alpha, which is the same as blended onto black.
        // The alpha channel itself is ignored by the factory method.
        return keyFactory.FromBgra32Array(width, height, pixels);
    }
}
