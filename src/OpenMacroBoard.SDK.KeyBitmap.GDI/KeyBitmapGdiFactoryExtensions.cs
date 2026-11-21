using System;
using System.Drawing;
using System.Drawing.Imaging;

#pragma warning disable AV1505 // Namespace should match with assembly name

namespace OpenMacroBoard.SDK;

/// <summary>
/// Collection of factory extension methods for <see cref="KeyBitmap"/>s.
/// </summary>
/// <remarks>
/// <para>You typically don't want to invoke the static methods directly, but instead use them
/// as extension methods by calling <see cref="KeyBitmap.Create"/>.XYZ();</para>
/// </remarks>
public static class KeyBitmapGdiFactoryExtensions
{
    /// <summary>
    /// Create key bitmap from graphics commands (for example with lambda expression)
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="width">width of the bitmap</param>
    /// <param name="height">height of the bitmap</param>
    /// <param name="graphicsAction">action that draws on the <see cref="Graphics"/></param>
    /// <exception cref="ArgumentNullException">
    /// Is thrown if <paramref name="graphicsAction"/> is null.
    /// </exception>
    public static KeyBitmap FromGraphics(
        this IKeyBitmapFactory keyFactory,
        int width,
        int height,
        Action<Graphics> graphicsAction
    )
    {
        ArgumentNullException.ThrowIfNull(graphicsAction);

        using var bmp = CreateKeyBitmap(width, height);

        using (var g = Graphics.FromImage(bmp))
        {
            graphicsAction(g);
        }

        return keyFactory.FromBitmap(bmp);
    }

    /// <summary>
    /// Creates a <see cref="KeyBitmap"/> from a given <see cref="Bitmap"/>
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="bitmap">The bitmap used to create the <see cref="KeyBitmap"/></param>
    /// <exception cref="ArgumentNullException">
    /// Is thrown if <paramref name="bitmap"/> is null.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Is thrown if the pixel format is not supported. Currently the only two allowed pixel formats
    /// are <see cref="PixelFormat.Format32bppArgb"/> and <see cref="PixelFormat.Format24bppRgb"/>.
    /// </exception>
    public static KeyBitmap FromBitmap(this IKeyBitmapFactory keyFactory, Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var w = bitmap.Width;
        var h = bitmap.Height;

        BitmapData? data = null;

        try
        {
            data = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var managedBGR = new byte[w * h * 3];

            unsafe
            {
                var bdata = (byte*)data.Scan0;

                if (data.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    for (var y = 0; y < h; y++)
                    {
                        for (var x = 0; x < w; x++)
                        {
                            var ps = data.Stride * y + x * 3;
                            var pt = (w * y + x) * 3;
                            managedBGR[pt + 0] = bdata[ps + 0];
                            managedBGR[pt + 1] = bdata[ps + 1];
                            managedBGR[pt + 2] = bdata[ps + 2];
                        }
                    }
                }
                else if (data.PixelFormat == PixelFormat.Format32bppArgb)
                {
                    for (var y = 0; y < h; y++)
                    {
                        for (var x = 0; x < w; x++)
                        {
                            var ps = data.Stride * y + x * 4;
                            var pt = (w * y + x) * 3;
                            var alpha = (double)bdata[ps + 3] / 255f;
                            managedBGR[pt + 0] = (byte)Math.Round(bdata[ps + 0] * alpha);
                            managedBGR[pt + 1] = (byte)Math.Round(bdata[ps + 1] * alpha);
                            managedBGR[pt + 2] = (byte)Math.Round(bdata[ps + 2] * alpha);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported pixel format");
                }
            }

            return keyFactory.FromBgr24Array(w, h, managedBGR);
        }
        finally
        {
            if (data != null)
            {
                bitmap.UnlockBits(data);
            }
        }
    }

    private static Bitmap CreateKeyBitmap(int width, int height)
    {
        return new Bitmap(width, height, PixelFormat.Format24bppRgb);
    }
}
