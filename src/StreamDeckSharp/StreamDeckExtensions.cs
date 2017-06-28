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
        public static void SetKeyBitmap(this IStreamDeck deck, int keyId, StreamDeckKeyBitmap bitmap)
        {
            deck.SetKeyBitmap(keyId, bitmap.rawBitmapData);
        }

        public static void ClearKey(this IStreamDeck deck, int keyId)
        {
            deck.SetKeyBitmap(keyId, StreamDeckKeyBitmap.Black);
        }

        public static void ClearKeys(this IStreamDeck deck)
        {
            for (int i = 0; i < StreamDeckHID.numOfKeys; i++)
                deck.SetKeyBitmap(i, StreamDeckKeyBitmap.Black);
        }
    }
}
