using OpenMacroBoard.SDK.Helper;
using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Represents a grid-like keyboard layout for macro boards.
/// </summary>
public class GridKeyLayout : IKeyLayout
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GridKeyLayout"/> class.
    /// </summary>
    /// <param name="countX">Number of keys in the x-coordinate (horizontal)</param>
    /// <param name="countY">Number of keys in the y-coordinate (vertical)</param>
    /// <param name="keySize">Square key size (pixels)</param>
    /// <param name="gapSize">Distance between keys (pixels)</param>
    public GridKeyLayout(int countX, int countY, int keySize, int gapSize)
    {
#pragma warning disable SA1503, IDE0011 // Braces should not be omitted
        if (countX <= 0) throw new ArgumentOutOfRangeException(nameof(countX));
        if (countY <= 0) throw new ArgumentOutOfRangeException(nameof(countY));
        if (keySize <= 0) throw new ArgumentOutOfRangeException(nameof(keySize));
        if (gapSize <= 0) throw new ArgumentOutOfRangeException(nameof(gapSize));
#pragma warning restore SA1503, IDE0011

        CountX = countX;
        CountY = countY;
        KeySize = keySize;
        GapSize = gapSize;
        Count = countX * countY;

        Area = this.GetFullArea();
    }

    /// <summary>
    /// Gets the number of keys on this layout.
    /// </summary>
    public int Count { get; }

    /// <inheritdoc />
    public int KeySize { get; }

    /// <inheritdoc />
    public int GapSize { get; }

    /// <inheritdoc />
    public OmbRectangle Area { get; }

    /// <inheritdoc />
    public int CountX { get; }

    /// <inheritdoc />
    public int CountY { get; }

    /// <summary>
    /// Gets the dimensions of the key with a given <paramref name="keyIndex"/>.
    /// </summary>
    /// <param name="keyIndex">The index of the key.</param>
    /// <returns>The dimensions of the requested key.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Is thrown if the <paramref name="keyIndex"/> is out of range.</exception>
    public OmbRectangle this[int keyIndex]
    {
        get
        {
            if (keyIndex < 0 || keyIndex >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(keyIndex));
            }

            // split id into x and y component
            var y = keyIndex / CountX;
            var x = keyIndex % CountX;

            var fullSize = KeySize + GapSize;
            return new OmbRectangle(fullSize * x, fullSize * y, KeySize, KeySize);
        }
    }

    /// <inheritdoc />
    public IEnumerator<OmbRectangle> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
