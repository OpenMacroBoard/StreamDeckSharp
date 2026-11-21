using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Represents a bitmap that can be used as key images
/// </summary>
public sealed class KeyBitmap : IKeyBitmapDataAccess
{
    private static readonly ImmutableArray<byte> BlackBgr24 = [0, 0, 0];

    private readonly ReadOnlyMemory<byte> rawBitmapData;

    internal KeyBitmap(int width, int height, ReadOnlyMemory<byte> bitmapData)
    {
        Debug.Assert(width >= 1, "Image width out of range");
        Debug.Assert(height >= 1, "Image height out of range");
        Debug.Assert(bitmapData.Length == width * height * 3, "Image data length mismatch");

        Width = width;
        Height = height;
        rawBitmapData = bitmapData;
    }

    /// <summary>
    /// This property can be used to create new KeyBitmaps
    /// </summary>
    /// <remarks>
    /// <para>This property just serves as an anchor point for extension methods
    /// to create new <see cref="KeyBitmap"/> objects</para>
    /// <para>
    /// The value of this property is not relevant (and set to null).
    /// </para>
    /// </remarks>
    public static IKeyBitmapFactory Create { get; } = null!;

    /// <summary>
    /// Solid black bitmap.
    /// </summary>
    /// <remarks>
    /// <para>If you need a black bitmap (for example to clear keys).</para>
    /// </remarks>
    public static KeyBitmap Black { get; } = new(1, 1, BlackBgr24.AsMemory());

    /// <summary>
    /// Gets the width of the bitmap.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the bitmap.
    /// </summary>
    public int Height { get; }

    /// <inheritdoc />
    ReadOnlyMemory<byte> IKeyBitmapDataAccess.GetData()
    {
        return rawBitmapData;
    }
}
