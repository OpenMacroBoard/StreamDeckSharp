using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Represents a color value.
/// </summary>
public readonly partial struct OmbColor : IEquatable<OmbColor>
{
    private OmbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// The red part of the color.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// The green part of the color.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// The blue part of the color.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Checks whether two <see cref="OmbColor"/> structures are equal.
    /// </summary>
    /// <param name="left">The left hand <see cref="OmbColor"/> operand.</param>
    /// <param name="right">The right hand <see cref="OmbColor"/> operand.</param>
    /// <returns>
    /// True if the <paramref name="left"/> parameter is equal to the <paramref name="right"/> parameter;
    /// otherwise, false.
    /// </returns>
    public static bool operator ==(OmbColor left, OmbColor right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks whether two <see cref="OmbColor"/> structures are equal.
    /// </summary>
    /// <param name="left">The left hand <see cref="OmbColor"/> operand.</param>
    /// <param name="right">The right hand <see cref="OmbColor"/> operand.</param>
    /// <returns>
    /// True if the <paramref name="left"/> parameter is not equal to the <paramref name="right"/> parameter;
    /// otherwise, false.
    /// </returns>
    public static bool operator !=(OmbColor left, OmbColor right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates a <see cref="OmbColor"/> from RGB bytes.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <returns>The <see cref="OmbColor"/>.</returns>
    public static OmbColor FromRgb(byte r, byte g, byte b)
    {
        return new OmbColor(r, g, b);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"#{R:X2}{G:X2}{B:X2}";
    }

    /// <inheritdoc />
    public bool Equals(OmbColor other)
    {
        return
            R == other.R &&
            G == other.G &&
            B == other.B;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is OmbColor other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B);
    }
}
