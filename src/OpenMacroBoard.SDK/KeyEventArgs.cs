using System;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// An event argument that is used to communicate key state changes.
    /// </summary>
    public class KeyEventArgs : EventArgs
    {
        /// <summary>
        /// The index of the key that was pressed or released.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// A flag that determines if the key was pressed or released.
        /// </summary>
        public bool IsDown { get; }

        /// <summary>
        /// Constructs a new <see cref="KeyEventArgs"/> object.
        /// </summary>
        /// <param name="key">The index of the key that was pressed or released.</param>
        /// <param name="isDown">A flag that determines if the key was pressed or released.</param>
        public KeyEventArgs(int key, bool isDown)
        {
            Key = key;
            IsDown = isDown;
        }
    }
}
