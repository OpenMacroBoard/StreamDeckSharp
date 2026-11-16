using System;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// The result of an image decoder: pixels as blue, green, red, alpha bytes, row by row from top to bottom.
/// </summary>
internal sealed class DecodedImage
{
    /// <summary>
    /// Number of bytes per pixel in <see cref="Bgra"/>.
    /// </summary>
    public const int ChannelCount = 4;

    /// <summary>
    /// Images with more pixels are rejected, so a small damaged or hostile file
    /// cannot make the decoder allocate a huge amount of memory.
    /// </summary>
    public const int MaxPixelCount = 100_000_000;

    public DecodedImage(int width, int height)
    {
        EnsureValidSize(width, height);

        Width = width;
        Height = height;
        Bgra = new byte[checked(width * height * ChannelCount)];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Bgra { get; }

    /// <summary>
    /// Throws if the size is not positive or the image has too many pixels.
    /// </summary>
    /// <exception cref="ImageDecodingException">The size is not valid.</exception>
    public static void EnsureValidSize(long width, long height)
    {
        if (width < 1 || height < 1)
        {
            throw new ImageDecodingException($"Invalid image size ({width} x {height}).");
        }

        if (width * height > MaxPixelCount)
        {
            throw new ImageDecodingException(
                $"The image is too large ({width} x {height}). At most {MaxPixelCount} pixels are supported."
            );
        }
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        var index = ((y * Width) + x) * ChannelCount;

        Bgra[index] = b;
        Bgra[index + 1] = g;
        Bgra[index + 2] = r;
        Bgra[index + 3] = a;
    }

    /// <summary>
    /// Converts to BGR24. Transparent pixels are blended onto black.
    /// </summary>
    public byte[] ToBgr24OnBlack()
    {
        const int targetChannelCount = 3;
        const int opaque = 255;

        var pixelCount = Width * Height;
        var result = new byte[checked(pixelCount * targetChannelCount)];
        ReadOnlySpan<byte> source = Bgra;

        for (var i = 0; i < pixelCount; i++)
        {
            var from = i * ChannelCount;
            var to = i * targetChannelCount;
            int alpha = source[from + 3];

            if (alpha == opaque)
            {
                source.Slice(from, targetChannelCount).CopyTo(result.AsSpan(to, targetChannelCount));
                continue;
            }

            for (var channel = 0; channel < targetChannelCount; channel++)
            {
                result[to + channel] = (byte)(((source[from + channel] * alpha) + (opaque / 2)) / opaque);
            }
        }

        return result;
    }
}
