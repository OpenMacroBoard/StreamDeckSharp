using System;

namespace StreamDeckSharp
{
    /// <summary>
    /// Represents a bitmap that can be used as key images
    /// </summary>
    public partial class KeyBitmap
    {
        /// <summary>
        /// Solid black bitmap
        /// </summary>
        /// <remarks>
        /// If you need a black bitmap (for example to clear keys) use this property for better performance (in theory ^^)
        /// </remarks>
        public static KeyBitmap Black { get { return black; } }
        private static readonly KeyBitmap black = new KeyBitmap(null);

        /// <remarks>
        /// The raw pixel format is a byte array of length 15552. This number is based on the image
        /// dimensions used by StreamDeck 72x72 pixels and 3 channels (RGB) for each pixel. 72 x 72 x 3 = 15552.
        /// 
        /// The channels are in the order BGR and the pixel rows (stride) are in reverse order.
        /// If you need some help try <see cref="KeyBitmap"/>
        /// </remarks>
        internal readonly byte[] rawBitmapData;

        internal KeyBitmap(byte[] bitmapData)
        {
            if (bitmapData != null)
            {
                if (bitmapData.Length != HidClient.rawBitmapDataLength) throw new NotSupportedException("Unsupported bitmap array length");
                this.rawBitmapData = bitmapData;
            }
        }
    }
}
