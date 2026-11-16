using OpenMacroBoard.SDK;

namespace StreamDeckSharp;

/// <summary>
/// Options for opening Stream Deck devices.
/// </summary>
public sealed record StreamDeckOptions
{
    /// <summary>
    /// Gets the options that are used when none are given.
    /// </summary>
    public static StreamDeckOptions Default { get; } = new();

    /// <summary>
    /// Gets the encoder for devices that expect key images as JPEG.
    /// </summary>
    /// <remarks>
    /// <para>The default is the built-in <see cref="OmbJpegEncoder"/>. Set this if you want to use
    /// a different JPEG encoder, for example one that your platform provides.</para>
    /// <para>The encoder is called on the thread that sets a key bitmap, so it has to be thread-safe
    /// if key bitmaps are set from several threads.</para>
    /// </remarks>
    public IJpegEncoder JpegEncoder { get; init; } = OmbJpegEncoder.Default;

    /// <summary>
    /// Gets a value indicating whether key bitmaps are written by a background task.
    /// </summary>
    /// <remarks>
    /// <para>With the write cache (the default) setting a key bitmap returns immediately, and a bitmap
    /// that is replaced before it was sent to the device is skipped. Without it every bitmap is
    /// sent to the device before the call returns.</para>
    /// </remarks>
    public bool UseWriteCache { get; init; } = true;
}
