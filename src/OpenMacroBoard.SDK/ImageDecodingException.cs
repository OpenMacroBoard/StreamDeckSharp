using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Thrown when an image cannot be decoded: the format is not supported,
/// the data is damaged or incomplete, or the image is too large.
/// </summary>
/// <remarks>
/// <para>This is the only exception the built-in image decoders throw for bad image data.</para>
/// </remarks>
public class ImageDecodingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDecodingException"/> class.
    /// </summary>
    public ImageDecodingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDecodingException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the problem.</param>
    public ImageDecodingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageDecodingException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the problem.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ImageDecodingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
