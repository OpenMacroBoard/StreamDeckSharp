using System;
using System.Runtime.CompilerServices;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Represents an ordered pair of integer x- and y-coordinates that defines a point in
/// a two-dimensional plane.
/// </summary>
public readonly struct OmbPoint : IEquatable<OmbPoint>
{
    /// <summary>
    /// Represents a <see cref="OmbPoint"/> that has X and Y values set to zero.
    /// </summary>
    public static readonly OmbPoint Empty = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbPoint"/> struct.
    /// </summary>
    /// <param name="x">The horizontal position of the point.</param>
    /// <param name="y">The vertical position of the point.</param>
    public OmbPoint(int x, int y)
        : this()
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbPoint"/> struct from the given <see cref="OmbSize"/>.
    /// </summary>
    /// <param name="size">The size.</param>
    public OmbPoint(OmbSize size)
    {
        X = size.Width;
        Y = size.Height;
    }

    /// <summary>
    /// Gets or sets the x-coordinate of this <see cref="OmbPoint"/>.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets or sets the y-coordinate of this <see cref="OmbPoint"/>.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="OmbPoint"/> is empty.
    /// </summary>
    public bool IsEmpty => Equals(Empty);

    /// <summary>
    /// Creates a <see cref="OmbSize"/> with the coordinates of the specified <see cref="OmbPoint"/>.
    /// </summary>
    /// <param name="point">The point.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator OmbSize(OmbPoint point)
    {
        return new OmbSize(point.X, point.Y);
    }

    /// <summary>
    /// Divides <see cref="OmbPoint"/> by a <see cref="int"/> producing <see cref="OmbPoint"/>.
    /// </summary>
    /// <param name="left">Dividend of type <see cref="OmbPoint"/>.</param>
    /// <param name="right">Divisor of type <see cref="int"/>.</param>
    /// <returns>Result of type <see cref="OmbPoint"/>.</returns>
    public static OmbPoint operator /(OmbPoint left, int right)
    {
        return new OmbPoint(left.X / right, left.Y / right);
    }

    /// <summary>
    /// Compares two <see cref="OmbPoint"/> objects for equality.
    /// </summary>
    /// <param name="left">The <see cref="OmbPoint"/> on the left side of the operand.</param>
    /// <param name="right">The <see cref="OmbPoint"/> on the right side of the operand.</param>
    /// <returns>
    /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(OmbPoint left, OmbPoint right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="OmbPoint"/> objects for inequality.
    /// </summary>
    /// <param name="left">The <see cref="OmbPoint"/> on the left side of the operand.</param>
    /// <param name="right">The <see cref="OmbPoint"/> on the right side of the operand.</param>
    /// <returns>
    /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(OmbPoint left, OmbPoint right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Deconstructs this point into two integers.
    /// </summary>
    /// <param name="x">The out value for X.</param>
    /// <param name="y">The out value for Y.</param>
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Point [ X={X}, Y={Y} ]";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is OmbPoint other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(OmbPoint other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }
}
