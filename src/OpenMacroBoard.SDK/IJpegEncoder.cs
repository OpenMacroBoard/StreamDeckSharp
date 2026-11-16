using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Encodes raw pixel data as JPEG.
/// </summary>
/// <remarks>
/// <para>Device providers that have to send JPEG images to a device use this interface, so the
/// encoder can be replaced, for example on platforms that bring their own JPEG encoder.
/// <see cref="OmbJpegEncoder"/> is the built-in implementation.</para>
/// <para>Implementations must be thread-safe: <see cref="Encode"/> is called on the thread
/// that sets a key bitmap, and that can be several threads at the same time.</para>
/// </remarks>
public interface IJpegEncoder
{
    /// <summary>
    /// Encodes an image as JPEG.
    /// </summary>
    /// <param name="bitmapDataBgr24">
    /// The pixels as blue, green, red bytes, row by row from top to bottom, without padding.
    /// The length is <paramref name="width"/> x <paramref name="height"/> x 3.
    /// </param>
    /// <param name="width">Width of the image in pixels.</param>
    /// <param name="height">Height of the image in pixels.</param>
    /// <returns>The complete JPEG file as bytes.</returns>
    byte[] Encode(ReadOnlySpan<byte> bitmapDataBgr24, int width, int height);
}
