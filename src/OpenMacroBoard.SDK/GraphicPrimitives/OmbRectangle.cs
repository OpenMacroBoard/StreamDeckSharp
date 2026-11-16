using System;
using System.Runtime.CompilerServices;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Stores a set of four integers that represent the location and size of a rectangle.
/// </summary>
public readonly struct OmbRectangle : IEquatable<OmbRectangle>
{
    /// <summary>
    /// Represents a <see cref="OmbRectangle"/> that has X, Y, Width, and Height values set to zero.
    /// </summary>
    public static readonly OmbRectangle Empty = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbRectangle"/> struct.
    /// </summary>
    /// <param name="x">The horizontal position of the rectangle.</param>
    /// <param name="y">The vertical position of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public OmbRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbRectangle"/> struct.
    /// </summary>
    /// <param name="point">
    /// The <see cref="OmbPoint"/> which specifies the rectangles point in a two-dimensional plane.
    /// </param>
    /// <param name="size">
    /// The <see cref="OmbSize"/> which specifies the rectangles height and width.
    /// </param>
    public OmbRectangle(OmbPoint point, OmbSize size)
    {
        X = point.X;
        Y = point.Y;
        Width = size.Width;
        Height = size.Height;
    }

    /// <summary>
    /// Gets or sets the x-coordinate of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets or sets the y-coordinate of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets or sets the width of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets or sets the height of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets or sets the coordinates of the upper-left corner of the rectangular region represented by this <see cref="OmbRectangle"/>.
    /// </summary>
    public OmbPoint Location
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(X, Y);
    }

    /// <summary>
    /// Gets or sets the size of this <see cref="OmbRectangle"/>.
    /// </summary>
    public OmbSize Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Width, Height);
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="OmbRectangle"/> is empty.
    /// </summary>
    public bool IsEmpty => Equals(Empty);

    /// <summary>
    /// Gets the y-coordinate of the top edge of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Top => Y;

    /// <summary>
    /// Gets the x-coordinate of the right edge of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked(X + Width);
    }

    /// <summary>
    /// Gets the y-coordinate of the bottom edge of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Bottom
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked(Y + Height);
    }

    /// <summary>
    /// Gets the x-coordinate of the left edge of this <see cref="OmbRectangle"/>.
    /// </summary>
    public int Left => X;

    /// <summary>
    /// Compares two <see cref="OmbRectangle"/> objects for equality.
    /// </summary>
    /// <param name="left">The <see cref="OmbRectangle"/> on the left side of the operand.</param>
    /// <param name="right">The <see cref="OmbRectangle"/> on the right side of the operand.</param>
    /// <returns>
    /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(OmbRectangle left, OmbRectangle right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="OmbRectangle"/> objects for inequality.
    /// </summary>
    /// <param name="left">The <see cref="OmbRectangle"/> on the left side of the operand.</param>
    /// <param name="right">The <see cref="OmbRectangle"/> on the right side of the operand.</param>
    /// <returns>
    /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(OmbRectangle left, OmbRectangle right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates a rectangle from left, top, right, bottom.
    /// </summary>
    public static OmbRectangle FromLTRB(int left, int top, int right, int bottom)
    {
        return new OmbRectangle(left, top, unchecked(right - left), unchecked(bottom - top));
    }

    /// <summary>
    /// Deconstructs this rectangle into four integers.
    /// </summary>
    /// <param name="x">The out value for X.</param>
    /// <param name="y">The out value for Y.</param>
    /// <param name="width">The out value for the width.</param>
    /// <param name="height">The out value for the height.</param>
    public void Deconstruct(out int x, out int y, out int width, out int height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(OmbRectangle other)
    {
        return
            X.Equals(other.X) &&
            Y.Equals(other.Y) &&
            Width.Equals(other.Width) &&
            Height.Equals(other.Height);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is OmbRectangle other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Rectangle [ X={X}, Y={Y}, Width={Width}, Height={Height} ]";
    }
}
