using System;
using System.Runtime.CompilerServices;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Stores an ordered pair of integers, which specify a height and width.
/// </summary>
public readonly struct OmbSize : IEquatable<OmbSize>
{
    /// <summary>
    /// Represents a <see cref="OmbSize"/> that has Width and Height values set to zero.
    /// </summary>
    public static readonly OmbSize Empty = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbSize"/> struct.
    /// </summary>
    /// <param name="value">The width and height of the size.</param>
    public OmbSize(int value)
        : this()
    {
        Width = value;
        Height = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbSize"/> struct.
    /// </summary>
    /// <param name="width">The width of the size.</param>
    /// <param name="height">The height of the size.</param>
    public OmbSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbSize"/> struct.
    /// </summary>
    /// <param name="size">The size.</param>
    public OmbSize(OmbSize size)
        : this()
    {
        Width = size.Width;
        Height = size.Height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OmbSize"/> struct from the given <see cref="OmbPoint"/>.
    /// </summary>
    /// <param name="point">The point.</param>
    public OmbSize(OmbPoint point)
    {
        Width = point.X;
        Height = point.Y;
    }

    /// <summary>
    /// Gets or sets the width of this <see cref="OmbSize"/>.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets or sets the height of this <see cref="OmbSize"/>.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="OmbSize"/> is empty.
    /// </summary>
    public bool IsEmpty => Equals(Empty);

    /// <summary>
    /// Converts the given <see cref="OmbSize"/> into a <see cref="OmbPoint"/>.
    /// </summary>
    /// <param name="size">The size.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator OmbPoint(OmbSize size)
    {
        return new OmbPoint(size.Width, size.Height);
    }

    /// <summary>
    /// Compares two <see cref="OmbSize"/> objects for equality.
    /// </summary>
    /// <param name="left">
    /// The <see cref="OmbSize"/> on the left side of the operand.
    /// </param>
    /// <param name="right">
    /// The <see cref="OmbSize"/> on the right side of the operand.
    /// </param>
    /// <returns>
    /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(OmbSize left, OmbSize right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="OmbSize"/> objects for inequality.
    /// </summary>
    /// <param name="left">
    /// The <see cref="OmbSize"/> on the left side of the operand.
    /// </param>
    /// <param name="right">
    /// The <see cref="OmbSize"/> on the right side of the operand.
    /// </param>
    /// <returns>
    /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(OmbSize left, OmbSize right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Deconstructs this size into two integers.
    /// </summary>
    /// <param name="width">The out value for the width.</param>
    /// <param name="height">The out value for the height.</param>
    public void Deconstruct(out int width, out int height)
    {
        width = Width;
        height = Height;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Size [ Width={Width}, Height={Height} ]";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is OmbSize other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(OmbSize other)
    {
        return Width.Equals(other.Width) && Height.Equals(other.Height);
    }
}
