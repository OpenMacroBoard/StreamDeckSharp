using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Collection of factory extension methods for <see cref="KeyBitmap"/>s.
/// </summary>
/// <remarks>
/// <para>You typically don't want to invoke the static methods directly, but instead use them
/// as extension methods by calling <see cref="KeyBitmap.Create"/>.XYZ();</para>
/// </remarks>
public static class KeyBitmapBasicFactoryExtensions
{
    /// <summary>
    /// Creates a new <see cref="KeyBitmap"/> object.
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="width">width of the bitmap</param>
    /// <param name="height">height of the bitmap</param>
    /// <param name="bitmapDataBgr24">raw bitmap data (Bgr24)</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either <paramref name="width"/> or <paramref name="height"/> are smaller than one.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Provided <paramref name="width"/> and <paramref name="height"/> doesn't match the
    /// expected array length of <paramref name="bitmapDataBgr24"/>.
    /// </exception>
    public static KeyBitmap FromBgr24Array(
        this IKeyBitmapFactory keyFactory,
        int width,
        int height,
        ReadOnlySpan<byte> bitmapDataBgr24
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var expectedLength = width * height * 3;

        if (bitmapDataBgr24.Length != expectedLength)
        {
            throw new ArgumentException($"{nameof(bitmapDataBgr24)}.Length does not match it's expected size ({nameof(width)} x {nameof(height)} x 3)", nameof(bitmapDataBgr24));
        }

        var data = new byte[expectedLength];
        bitmapDataBgr24.CopyTo(data);

        return new KeyBitmap(width, height, data);
    }

    /// <summary>
    /// Creates a new <see cref="KeyBitmap"/> object.
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="width">width of the bitmap</param>
    /// <param name="height">height of the bitmap</param>
    /// <param name="bitmapDataBgra32">raw bitmap data (Bgra32). The alpha channel will be ignored.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either <paramref name="width"/> or <paramref name="height"/> are smaller than one.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Provided <paramref name="width"/> and <paramref name="height"/> doesn't match the
    /// expected array length of <paramref name="bitmapDataBgra32"/>.
    /// </exception>
    public static KeyBitmap FromBgra32Array(
        this IKeyBitmapFactory keyFactory,
        int width,
        int height,
        ReadOnlySpan<byte> bitmapDataBgra32
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var pixelCount = width * height;
        var expectedLength = pixelCount * 4;

        if (bitmapDataBgra32.Length != expectedLength)
        {
            throw new ArgumentException($"{nameof(bitmapDataBgra32)}.Length does not match it's expected size ({nameof(width)} x {nameof(height)} x 4)", nameof(bitmapDataBgra32));
        }

        var data = new byte[pixelCount * 3];

        for (int i = 0; i < pixelCount; i++)
        {
            var i3 = i * 3;
            var i4 = i * 4;

            // Copy BGR and ignore alpha channel
            data[i3 + 0] = bitmapDataBgra32[i4 + 0];
            data[i3 + 1] = bitmapDataBgra32[i4 + 1];
            data[i3 + 2] = bitmapDataBgra32[i4 + 2];
        }

        return new KeyBitmap(width, height, data);
    }

    /// <summary>
    /// Creates a new <see cref="KeyBitmap"/> object.
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="width">width of the bitmap</param>
    /// <param name="height">height of the bitmap</param>
    /// <param name="bitmapDataRgba32">raw bitmap data (Bgra32). The alpha channel will be ignored.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either <paramref name="width"/> or <paramref name="height"/> are smaller than one.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Provided <paramref name="width"/> and <paramref name="height"/> doesn't match the
    /// expected array length of <paramref name="bitmapDataRgba32"/>.
    /// </exception>
    public static KeyBitmap FromRgba32Array(
        this IKeyBitmapFactory keyFactory,
        int width,
        int height,
        ReadOnlySpan<byte> bitmapDataRgba32
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var pixelCount = width * height;
        var expectedLength = pixelCount * 4;

        if (bitmapDataRgba32.Length != expectedLength)
        {
            throw new ArgumentException($"{nameof(bitmapDataRgba32)}.Length does not match it's expected size ({nameof(width)} x {nameof(height)} x 4)", nameof(bitmapDataRgba32));
        }

        var data = new byte[pixelCount * 3];

        for (int i = 0; i < pixelCount; i++)
        {
            var i3 = i * 3;
            var i4 = i * 4;

            // Copy BGR and ignore alpha channel
            data[i3 + 2] = bitmapDataRgba32[i4 + 0];
            data[i3 + 1] = bitmapDataRgba32[i4 + 1];
            data[i3 + 0] = bitmapDataRgba32[i4 + 2];
        }

        return new KeyBitmap(width, height, data);
    }

    /// <summary>
    /// Creates a single color (single pixel) <see cref="KeyBitmap"/> with a given color.
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    public static KeyBitmap FromRgb(this IKeyBitmapFactory keyFactory, byte r, byte g, byte b)
    {
        // If everything is 0 (black) take a shortcut ;-)
        if (r == 0 && g == 0 && b == 0)
        {
            return KeyBitmap.Black;
        }

        var buffer = new byte[3] { b, g, r };
        return KeyBitmap.Create.FromBgr24Array(1, 1, buffer);
    }

    /// <summary>
    /// Creates a single color (single pixel) <see cref="KeyBitmap"/> with a given color.
    /// </summary>
    /// <param name="keyFactory">The builder that is used to create the <see cref="KeyBitmap"/></param>
    /// <param name="color">The color.</param>
    public static KeyBitmap FromColor(this IKeyBitmapFactory keyFactory, OmbColor color)
    {
        return keyFactory.FromRgb(color.R, color.G, color.B);
    }

    /// <summary>
    /// Create a bitmap from an encoded given image stream.
    /// </summary>
    public static KeyBitmap FromStream(this IKeyBitmapFactory builder, Stream bitmapStream)
    {
        return builder.FromImageSharpImage(Image.Load(bitmapStream));
    }

    /// <summary>
    /// Create a bitmap from an encoded given image stream.
    /// </summary>
    public static async Task<KeyBitmap> FromStreamAsync(this IKeyBitmapFactory builder, Stream bitmapStream)
    {
        return builder.FromImageSharpImage(await Image.LoadAsync(bitmapStream));
    }

    /// <summary>
    /// Create a bitmap from an encoded given image file.
    /// </summary>
    public static KeyBitmap FromFile(this IKeyBitmapFactory builder, string bitmapFile)
    {
        return builder.FromImageSharpImage(Image.Load(bitmapFile));
    }

    /// <summary>
    /// Create a bitmap from an encoded given image file.
    /// </summary>
    public static async Task<KeyBitmap> FromFileAsync(this IKeyBitmapFactory builder, string bitmapFile)
    {
        return builder.FromImageSharpImage(await Image.LoadAsync(bitmapFile));
    }

    /// <summary>
    /// Creates a <see cref="KeyBitmap"/> from a given <see cref="Image{Bgr24}"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The provided bitmap is null.</exception>
    /// <exception cref="NotSupportedException">The pixel format of the image is not supported.</exception>
    public static KeyBitmap FromImageSharpImage(this IKeyBitmapFactory keyFactory, Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        using var ctx = image.WithBgr24();
        var pixelData = ctx.Item.ToBgr24PixelArray();
        return KeyBitmap.Create.FromBgr24Array(image.Width, image.Height, pixelData);
    }
}
