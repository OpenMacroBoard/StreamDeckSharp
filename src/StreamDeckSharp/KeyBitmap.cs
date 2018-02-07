using System;
using System.Security.Cryptography;

namespace StreamDeckSharp
{
    /// <summary>
    /// Represents a bitmap that can be used as key images
    /// </summary>
    public partial class KeyBitmap : IEquatable<KeyBitmap>
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
        private int? cachedHashCode = null;

        internal KeyBitmap(byte[] bitmapData)
        {
            if (bitmapData != null)
            {
                if (bitmapData.Length != HidClient.rawBitmapDataLength)
                    throw new NotSupportedException("Unsupported bitmap array length");

                rawBitmapData = bitmapData;
            }
        }

        public static bool operator ==(KeyBitmap a, KeyBitmap b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null)) return false;
            if (ReferenceEquals(b, null)) return false;

            //Both "clear-button" (null) -> true
            if (a.rawBitmapData == null && b.rawBitmapData == null) return true;

            //If only one is "clear-button" (null) -> false
            if (a.rawBitmapData == null || b.rawBitmapData == null) return false;

            //compare bitmap content
            for (int i = 0; i < HidClient.rawBitmapDataLength; i++)
                if (a.rawBitmapData[i] != b.rawBitmapData[i])
                    return false;

            return true;
        }

        public static bool operator !=(KeyBitmap a, KeyBitmap b)
            => !(a == b);

        public bool Equals(KeyBitmap other)
            => this == other;

        public override bool Equals(object obj)
            => Equals(obj as KeyBitmap);

        public override int GetHashCode()
        {
            if (cachedHashCode != null)
                return cachedHashCode.Value;

            var h = CalculateObjectHash();
            cachedHashCode = h;
            return h;
        }

        /// <summary>
        /// Calculates the SHA1 hash of bitmap and returns only 32bit (object HashCode)
        /// </summary>
        /// <returns></returns>
        private int CalculateObjectHash()
        {
            if (rawBitmapData == null)
                return 0;

            //Use the first four bytes of the sha1 hash as object HashCode
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] hash = sha1.ComputeHash(rawBitmapData);
                return BitConverter.ToInt32(hash, 0);
            }
        }
    }
}
