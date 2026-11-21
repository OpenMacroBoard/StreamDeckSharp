using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Decodes image files to <see cref="KeyBitmap"/>s.
/// </summary>
/// <remarks>
/// <para><see cref="OmbImageDecoder"/> is the built-in implementation. Implement this interface to
/// load formats it does not support, with an image library of your choice, and pass it to
/// <c>KeyBitmap.Create.FromFile</c> or <c>KeyBitmap.Create.FromStream</c>.</para>
/// </remarks>
public interface IImageDecoder
{
    /// <summary>
    /// Decodes an image file.
    /// </summary>
    /// <param name="encodedImage">The complete content of the image file.</param>
    /// <returns>The decoded image.</returns>
    KeyBitmap Decode(ReadOnlySpan<byte> encodedImage);
}
