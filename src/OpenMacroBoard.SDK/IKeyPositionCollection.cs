using System.Collections.Generic;
using System.Drawing;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// Contains information about how the keys are layed out.
    /// </summary>
    public interface IKeyPositionCollection
        : IEnumerable<Rectangle>
    {
        /// <summary>
        /// Gets the number of keys for this layout.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the exact position of the key.
        /// </summary>
        /// <param name="keyIndex">Index of the key.</param>
        /// <returns>The position rectangle.</returns>
        Rectangle this[int keyIndex] { get; }

        /// <summary>
        /// A shortcut to get the total key layout area.
        /// The smallest rectangle that fits all keys.
        /// </summary>
        Rectangle Area { get; }
    }
}
