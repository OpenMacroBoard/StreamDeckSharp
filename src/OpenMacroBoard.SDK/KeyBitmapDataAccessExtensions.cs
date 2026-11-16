using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Extension methods for <see cref="IKeyBitmapDataAccess"/>.
/// </summary>
public static class KeyBitmapDataAccessExtensions
{
    /// <summary>
    /// Creates a new <see cref="Image{Bgr24}"/> for this <see cref="IKeyBitmapDataAccess"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keep in mind that this operation allocates and creates a copy under the hood.
    /// </para>
    /// </remarks>
    public static Image<Bgr24> ToImage(this IKeyBitmapDataAccess dataAccess)
    {
        return Image.LoadPixelData<Bgr24>(
            dataAccess.GetData().ToArray(),
            dataAccess.Width,
            dataAccess.Height
        );
    }
}
