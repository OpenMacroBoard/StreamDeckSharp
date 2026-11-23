using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Tells which key got a new image.
/// </summary>
public class KeyBitmapChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyBitmapChangedEventArgs"/> class.
    /// </summary>
    /// <param name="key">The index of the key that got a new image.</param>
    public KeyBitmapChangedEventArgs(int key)
    {
        Key = key;
    }

    /// <summary>
    /// Gets the index of the key that got a new image.
    /// </summary>
    public int Key { get; }
}
