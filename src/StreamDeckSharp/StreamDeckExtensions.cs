using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckSharp
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// The <see cref="IStreamDeck"/> interface is pretty basic to simplify implementation.
    /// This extension class adds some commonly used functions to make things simpler.
    /// </remarks>
    public static class StreamDeckExtensions
    {
        /// <summary>
        /// Sets a background image for a given key
        /// </summary>
        /// <param name="deck"></param>
        /// <param name="keyId"></param>
        /// <param name="bitmap"></param>
        public static void SetKeyBitmap(this IStreamDeck deck, int keyId, StreamDeckKeyBitmap bitmap)
        {
            deck.SetKeyBitmap(keyId, bitmap.rawBitmapData);
        }

        /// <summary>
        /// Sets a background image for all keys
        /// </summary>
        /// <param name="deck"></param>
        /// <param name="bitmap"></param>
        public static void SetKeyBitmap(this IStreamDeck deck, StreamDeckKeyBitmap bitmap)
        {
            for (int i = 0; i < StreamDeckHID.numOfKeys; i++)
                deck.SetKeyBitmap(i, bitmap.rawBitmapData);
        }

        /// <summary>
        /// Sets a background image for all keys
        /// </summary>
        /// <param name="deck"></param>
        /// <param name="bitmap"></param>
        public static void SetKeyBitmap(this IStreamDeck deck, byte[] bitmap)
        {
            for (int i = 0; i < StreamDeckHID.numOfKeys; i++)
                deck.SetKeyBitmap(i, bitmap);
        }

        /// <summary>
        /// Sets background to black for a given key
        /// </summary>
        /// <param name="deck"></param>
        /// <param name="keyId"></param>
        public static void ClearKey(this IStreamDeck deck, int keyId)
        {
            deck.SetKeyBitmap(keyId, StreamDeckKeyBitmap.Black);
        }

        /// <summary>
        /// Sets background to black for all given keys
        /// </summary>
        /// <param name="deck"></param>
        public static void ClearKeys(this IStreamDeck deck)
        {
            deck.SetKeyBitmap(StreamDeckKeyBitmap.Black);
        }
    }
}
