using OpenMacroBoard.SDK.Imaging;
using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Extension methods to scale, rotate and flip <see cref="KeyBitmap"/>s.
/// </summary>
/// <remarks>
/// <para>A <see cref="KeyBitmap"/> is immutable, so every method returns a new bitmap
/// (or the same one, if nothing has to change).</para>
/// </remarks>
public static class KeyBitmapTransformExtensions
{
    private const int ChannelCount = 3;

    /// <summary>
    /// Scales a bitmap to a new size. The aspect ratio is not preserved.
    /// </summary>
    /// <param name="keyBitmap">The bitmap to scale.</param>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    /// <returns>The scaled bitmap, or the same bitmap if it already has that size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either <paramref name="width"/> or <paramref name="height"/> are smaller than one.
    /// </exception>
    public static KeyBitmap Resize(this KeyBitmap keyBitmap, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        if (keyBitmap.Width == width && keyBitmap.Height == height)
        {
            return keyBitmap;
        }

        var data = Bgr24Resampler.Resize(GetData(keyBitmap), keyBitmap.Width, keyBitmap.Height, width, height);
        return new KeyBitmap(width, height, data);
    }

    /// <summary>
    /// Rotates a bitmap by 90 degrees clockwise. Width and height are swapped.
    /// </summary>
    /// <param name="keyBitmap">The bitmap to rotate.</param>
    /// <returns>The rotated bitmap.</returns>
    public static KeyBitmap Rotate90(this KeyBitmap keyBitmap)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);

        var sourceHeight = keyBitmap.Height;

        return Map(
            keyBitmap,
            keyBitmap.Height,
            keyBitmap.Width,
            (x, y) => (y, sourceHeight - 1 - x)
        );
    }

    /// <summary>
    /// Rotates a bitmap by 180 degrees.
    /// </summary>
    /// <param name="keyBitmap">The bitmap to rotate.</param>
    /// <returns>The rotated bitmap.</returns>
    public static KeyBitmap Rotate180(this KeyBitmap keyBitmap)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);

        var sourceWidth = keyBitmap.Width;
        var sourceHeight = keyBitmap.Height;

        return Map(
            keyBitmap,
            keyBitmap.Width,
            keyBitmap.Height,
            (x, y) => (sourceWidth - 1 - x, sourceHeight - 1 - y)
        );
    }

    /// <summary>
    /// Rotates a bitmap by 270 degrees clockwise (90 degrees counterclockwise).
    /// Width and height are swapped.
    /// </summary>
    /// <param name="keyBitmap">The bitmap to rotate.</param>
    /// <returns>The rotated bitmap.</returns>
    public static KeyBitmap Rotate270(this KeyBitmap keyBitmap)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);

        var sourceWidth = keyBitmap.Width;

        return Map(
            keyBitmap,
            keyBitmap.Height,
            keyBitmap.Width,
            (x, y) => (sourceWidth - 1 - y, x)
        );
    }

    /// <summary>
    /// Mirrors a bitmap so that left and right are swapped.
    /// </summary>
    /// <param name="keyBitmap">The bitmap to mirror.</param>
    /// <returns>The mirrored bitmap.</returns>
    public static KeyBitmap FlipHorizontal(this KeyBitmap keyBitmap)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);

        var sourceWidth = keyBitmap.Width;

        return Map(
            keyBitmap,
            keyBitmap.Width,
            keyBitmap.Height,
            (x, y) => (sourceWidth - 1 - x, y)
        );
    }

    /// <summary>
    /// Mirrors a bitmap so that top and bottom are swapped.
    /// </summary>
    /// <param name="keyBitmap">The bitmap to mirror.</param>
    /// <returns>The mirrored bitmap.</returns>
    public static KeyBitmap FlipVertical(this KeyBitmap keyBitmap)
    {
        ArgumentNullException.ThrowIfNull(keyBitmap);

        var sourceHeight = keyBitmap.Height;

        return Map(
            keyBitmap,
            keyBitmap.Width,
            keyBitmap.Height,
            (x, y) => (x, sourceHeight - 1 - y)
        );
    }

    private static ReadOnlySpan<byte> GetData(KeyBitmap keyBitmap)
    {
        return ((IKeyBitmapDataAccess)keyBitmap).GetData().Span;
    }

    /// <summary>
    /// Creates a new bitmap where every pixel is taken from a position of the source bitmap.
    /// </summary>
    /// <param name="keyBitmap">The source bitmap.</param>
    /// <param name="width">Width of the new bitmap.</param>
    /// <param name="height">Height of the new bitmap.</param>
    /// <param name="sourcePosition">
    /// Returns the position in the source bitmap for a position in the new bitmap.
    /// </param>
    private static KeyBitmap Map(
        KeyBitmap keyBitmap,
        int width,
        int height,
        Func<int, int, (int X, int Y)> sourcePosition
    )
    {
        var source = GetData(keyBitmap);
        var sourceWidth = keyBitmap.Width;
        var data = new byte[source.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (sourceX, sourceY) = sourcePosition(x, y);

                var from = ((sourceY * sourceWidth) + sourceX) * ChannelCount;
                var to = ((y * width) + x) * ChannelCount;

                source.Slice(from, ChannelCount).CopyTo(data.AsSpan(to, ChannelCount));
            }
        }

        return new KeyBitmap(width, height, data);
    }
}
