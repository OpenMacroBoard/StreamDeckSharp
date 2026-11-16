using System;
using System.IO;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Decodes image files. The format is detected from the first bytes of the data, not from a file name.
/// </summary>
internal static class ImageDecoder
{
    /// <summary>
    /// Checks whether the data starts like a file of a supported format.
    /// </summary>
    public static bool CanDecode(ReadOnlySpan<byte> data)
    {
        return PngDecoder.IsPng(data) || BmpDecoder.IsBmp(data) || JpegDecoder.IsJpeg(data);
    }

    /// <summary>
    /// Decodes a BMP, PNG or JPEG file.
    /// </summary>
    /// <exception cref="ImageDecodingException">
    /// The format is not supported, the data is damaged or incomplete, or the image is too large.
    /// </exception>
    public static DecodedImage Decode(ReadOnlySpan<byte> data)
    {
        try
        {
            if (PngDecoder.IsPng(data))
            {
                return PngDecoder.Decode(data);
            }

            if (BmpDecoder.IsBmp(data))
            {
                return BmpDecoder.Decode(data);
            }

            if (JpegDecoder.IsJpeg(data))
            {
                return JpegDecoder.Decode(data);
            }
        }
        catch (Exception ex) when (IsCausedByBadData(ex))
        {
            // The decoders check what they read, but damaged data can still lead them outside of
            // a buffer. In safe code that is an exception and nothing worse; it is reported as bad data.
            throw new ImageDecodingException("The image data is damaged or incomplete.", ex);
        }

        throw new ImageDecodingException("Unknown image format. Supported formats are BMP, PNG and JPEG.");
    }

    private static bool IsCausedByBadData(Exception exception)
    {
        return exception
            is IndexOutOfRangeException
            or ArgumentException
            or InvalidDataException
            or EndOfStreamException
            or OverflowException
            or DivideByZeroException;
    }
}
