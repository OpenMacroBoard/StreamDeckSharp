using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// An interface that allows you to access the underlying data of <see cref="KeyBitmap"/>s.
/// </summary>
public interface IKeyBitmapDataAccess
{
    /// <summary>
    /// Gets the width of the bitmap.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the bitmap.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the underlying image data.
    /// </summary>
    ReadOnlyMemory<byte> GetData();
}
