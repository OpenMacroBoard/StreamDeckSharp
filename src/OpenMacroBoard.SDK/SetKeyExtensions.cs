using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A bunch of extensions to clear all keys, or set a single <see cref="KeyBitmap"/> to all keys.
/// </summary>
public static class SetKeyExtensions
{
    /// <summary>
    /// Sets a background image for all keys
    /// </summary>
    /// <exception cref="ArgumentNullException">The provided board is null.</exception>
    public static void SetKeyBitmap(this IMacroBoard board, KeyBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(board);

        for (var i = 0; i < board.Keys.Count; i++)
        {
            board.SetKeyBitmap(i, bitmap);
        }
    }

    /// <summary>
    /// Sets background to black for a given key
    /// </summary>
    /// <exception cref="ArgumentNullException">The provided board is null.</exception>
    public static void ClearKey(this IMacroBoard board, int keyId)
    {
        ArgumentNullException.ThrowIfNull(board);
        board.SetKeyBitmap(keyId, KeyBitmap.Black);
    }

    /// <summary>
    /// Sets background to black for all given keys
    /// </summary>
    public static void ClearKeys(this IMacroBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        board.SetKeyBitmap(KeyBitmap.Black);
    }
}
