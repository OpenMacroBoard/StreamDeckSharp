namespace OpenMacroBoard.SDK;

/// <summary>
/// Describes how a bitmap is fitted into a target size.
/// </summary>
public enum OmbResizeMode
{
    /// <summary>
    /// A bitmap that is smaller than the target size is centered without scaling.
    /// A larger bitmap is treated like <see cref="Pad"/>.
    /// </summary>
    BoxPad = 0,

    /// <summary>
    /// Scales the bitmap so that all of it fits into the target size, keeping its proportions.
    /// The rest of the target is black.
    /// </summary>
    Pad = 1,

    /// <summary>
    /// Scales the bitmap so that it covers the whole target size, keeping its proportions.
    /// What does not fit is cut off equally on both sides.
    /// </summary>
    Crop = 2,

    /// <summary>
    /// Scales the bitmap to exactly the target size, without keeping its proportions.
    /// </summary>
    Stretch = 3,
}
