using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckSharp
{
    /// <summary>
    /// Bare minimum StreamDeck Interface.
    /// </summary>
    public interface IStreamDeck : IDisposable
    {
        /// <summary>
        /// The number of keys present on the Stream Deck
        /// </summary>
        /// <remarks>
        /// At the moment there is only a Stream Deck device with 5x3 keys.
        /// But this may change in the future so please use this property in your
        /// code / for-loops.
        /// </remarks>
        int NumberOfKeys { get; }

        /// <summary>
        /// Is raised when a key is pressed
        /// </summary>
        event EventHandler<StreamDeckKeyEventArgs> KeyPressed;

        /// <summary>
        /// Sets the brightness for this <see cref="IStreamDeck"/>
        /// </summary>
        /// <param name="level">Brightness level (0 - 80~ish)</param>
        /// <remarks>
        /// At the moment it looks like the maximum level is about 80.
        /// Higher values seem to have no effect
        /// </remarks>
        void SetBrightness(byte level);

        /// <summary>
        /// Sets a background image for a given key
        /// </summary>
        /// <param name="keyId">Specifies which key the image will be applied on</param>
        /// <param name="bitmapData">The raw bitmap pixel data. Details see remarks section. The key will be painted black if this value is null.</param>
        /// <remarks>
        /// The raw pixel format is a byte array of length 15552. This number is based on the image
        /// dimensions used by StreamDeck 72x72 pixels and 3 channels (RGB) for each pixel. 72 x 72 x 3 = 15552.
        /// 
        /// The channels are in the order BGR and the pixel rows (stride) are in reverse order.
        /// If you need some help try <see cref="StreamDeckKeyBitmap"/>
        /// </remarks>
        void SetKeyBitmap(int keyId, byte[] bitmapData);
    }
}
