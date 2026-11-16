using OpenMacroBoard.SDK.Imaging;
using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// The built-in image decoder for BMP, PNG and JPEG files.
/// </summary>
/// <remarks>
/// <para>The format is detected from the content of the file, not from its name.
/// Transparent pixels are blended onto black.</para>
/// <para>Supported are uncompressed BMP files, PNG files with all color types and bit depths,
/// and baseline and progressive JPEG files in grayscale and color. Animation, color profiles,
/// gamma and the EXIF orientation are ignored. Images with more than 50 million pixels are rejected.</para>
/// <para>The decoder is written for safety and simplicity, not for speed: it uses no unsafe code.
/// It has no state and can be used from several threads.</para>
/// </remarks>
public sealed class OmbImageDecoder : IImageDecoder
{
    /// <summary>
    /// Gets the shared instance of the decoder.
    /// </summary>
    public static OmbImageDecoder Default { get; } = new();

    /// <inheritdoc/>
    /// <exception cref="ImageDecodingException">
    /// The format is not supported, the data is damaged or incomplete, or the image is too large.
    /// </exception>
    public KeyBitmap Decode(ReadOnlySpan<byte> encodedImage)
    {
        var decoded = ImageDecoder.Decode(encodedImage);
        return new KeyBitmap(decoded.Width, decoded.Height, decoded.ToBgr24OnBlack());
    }
}
