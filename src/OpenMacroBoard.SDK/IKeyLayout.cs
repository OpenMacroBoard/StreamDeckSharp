using System.Collections.Generic;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Describes the key layout of an <see cref="IMacroBoard"/>.
/// </summary>
public interface IKeyLayout : IReadOnlyList<OmbRectangle>
{
    /// <summary>
    /// Gets the image size of the keys that are supported.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In the rare case that the underlying board doesn't have square keys
    /// the value will be the size that most keys use, an average or a guess.
    /// If you need more accurate sizes you have the enumerate the key rectangles of
    /// this collection.
    /// </para>
    /// </remarks>
    int KeySize { get; }

    /// <summary>
    /// Gets the smallest rectangle area that fits all keys.
    /// </summary>
    OmbRectangle Area { get; }

    /// <summary>
    /// Gets the number of keys in the horizontal direction.
    /// </summary>
    /// <remarks>
    /// <para>In the rare case that the underlying board doesn't have a rectangular key layout
    /// this value might be an estimate or even wrong. It's not even guaranteed that
    /// <see cref="CountX"/> times <see cref="CountY"/> will be equal to Count
    /// but all implementations should make sure that the product is at least
    /// not greater than Count and <see cref="CountX"/> is at least 1.</para>
    /// </remarks>
    int CountX { get; }

    /// <summary>
    /// Gets the number of keys in the vertical direction.
    /// </summary>
    /// <remarks>
    /// <para>In the rare case that the underlying board doesn't have a rectangular key layout
    /// this value might be an estimate or even wrong. It's not even guaranteed that
    /// <see cref="CountX"/> times <see cref="CountY"/> will be equal to Count
    /// but all implementations should make sure that the product is at least
    /// not greater than Count and <see cref="CountY"/> is at least 1.</para>
    /// </remarks>
    int CountY { get; }

    /// <summary>
    /// Gets the gap between the keys.
    /// </summary>
    /// <remarks>
    /// <para>In the rare case that the underlying board doesn't have
    /// a rectangular key layout this value might be an estimate, made up or wrong.</para>
    /// </remarks>
    int GapSize { get; }
}
