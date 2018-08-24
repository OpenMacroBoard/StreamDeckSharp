using System;
using System.Linq;
using System.Security.Cryptography;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// Represents a bitmap that can be used as key images
    /// </summary>
    public sealed partial class KeyBitmap : IEquatable<KeyBitmap>, IKeyBitmapDataAccess
    {
        /// <summary>
        /// This property can be used to create new KeyBitmaps
        /// </summary>
        /// <remarks>
        /// This property just serves as an anchor point for extension methods
        /// to create new <see cref="KeyBitmap"/> objects
        /// </remarks>
        public static IKeyBitmapFactory Create { get; } = null;

        /// <summary>
        /// Solid black bitmap
        /// </summary>
        /// <remarks>
        /// If you need a black bitmap (for example to clear keys) use this property for better performance (in theory ^^)
        /// </remarks>
        public static KeyBitmap Black { get; } = new KeyBitmap(1, 1, null);

        /// <summary>
        /// Gets the width of the bitmap.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of the bitmap.
        /// </summary>
        public int Height { get; }

        int IKeyBitmapDataAccess.Stride
            => stride;

        int IKeyBitmapDataAccess.DataLength
            => rawBitmapData.Length;

        bool IKeyBitmapDataAccess.IsNull
            => rawBitmapData == null;

        /// <remarks>
        /// Byte order is B-G-R, and pixels are stored left-to-right and top-to-bottom
        /// </remarks>
        private readonly byte[] rawBitmapData;
        private readonly int stride;
        private int? cachedHashCode = null;

        /// <summary>
        /// Creates a new <see cref="KeyBitmap"/> object.
        /// </summary>
        /// <param name="width">width of the bitmap</param>
        /// <param name="height">height of the bitmap</param>
        /// <param name="bitmapData">raw bitmap data (Bgr24)</param>
        /// <remarks>
        /// Make sure you don't use or change the <paramref name="bitmapData"/> after constructing the object.
        /// This array is not copied for performance reasons and used by different threads.
        /// </remarks>
        public KeyBitmap(int width, int height, byte[] bitmapData)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height < 1)
                throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;

            if (bitmapData != null)
            {
                var expectedLength = width * height * 3;
                if (bitmapData.Length != expectedLength)
                    throw new ArgumentException($"{nameof(bitmapData)}.Length does not match it's expected size ({nameof(width)} x {nameof(height)} x 3)", nameof(bitmapData));

                stride = width * 3;
                rawBitmapData = (byte[])bitmapData.Clone();
            }
        }

        /// <summary>
        /// Compares the content of two given <see cref="KeyBitmap"/>s
        /// </summary>
        /// <param name="a">KeyBitmap a</param>
        /// <param name="b">KeyBitmap b</param>
        /// <returns>Returns true of the <see cref="KeyBitmap"/>s are equal and false otherwise.</returns>
        public static bool Equals(KeyBitmap a, KeyBitmap b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null)
                return false;

            if (b is null)
                return false;

            if (a.Width != b.Width)
                return false;

            if (a.Height != b.Height)
                return false;

            if (ReferenceEquals(a.rawBitmapData, b.rawBitmapData))
                return true;

            if (a.rawBitmapData is null)
                return false;

            if (b.rawBitmapData is null)
                return false;

            return Enumerable.SequenceEqual(a.rawBitmapData, b.rawBitmapData);
        }

        /// <summary>
        /// The == operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(KeyBitmap a, KeyBitmap b)
            => Equals(a, b);

        /// <summary>
        /// The != operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(KeyBitmap a, KeyBitmap b)
            => !Equals(a, b);

        /// <summary>
        /// Compares the content of this <see cref="KeyBitmap"/> to another KeyBitmap
        /// </summary>
        /// <param name="other">The other <see cref="KeyBitmap"/></param>
        /// <returns>True if both bitmaps are equals and false otherwise.</returns>
        public bool Equals(KeyBitmap other)
            => Equals(this, other);

        /// <summary>
        /// Compares the content of this <see cref="KeyBitmap"/> to another object
        /// </summary>
        /// <param name="obj">The other object</param>
        /// <returns>Return true if the other object is a <see cref="KeyBitmap"/> and equal to this one. Returns false otherwise.</returns>
        public override bool Equals(object obj)
            => Equals(this, obj as KeyBitmap);

        /// <summary>
        /// Get the hash code for this object.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            if (!cachedHashCode.HasValue)
                cachedHashCode = CalculateObjectHash();
            return cachedHashCode.Value;
        }

        private int CalculateObjectHash()
        {
            const int initalValue = 17;
            const int primeFactor = 23;
            const int imageSampleSize = 1000;

            unchecked
            {
                var hash = initalValue;
                hash = hash * primeFactor + Width;
                hash = hash * primeFactor + Height;

                if (rawBitmapData == null)
                    return hash;

                var stepSize = 1;
                if (rawBitmapData.Length > imageSampleSize)
                    stepSize = rawBitmapData.Length / imageSampleSize;

                for (int i = 0; i < rawBitmapData.Length; i += stepSize)
                {
                    hash *= 23;
                    hash += rawBitmapData[i];
                }

                return hash;
            }
        }

        void IKeyBitmapDataAccess.CopyData(byte[] targetArray, int targetIndex, int startIndex, int length)
        {
            Array.Copy(rawBitmapData, startIndex, targetArray, targetIndex, length);
        }

        void IKeyBitmapDataAccess.CopyData(byte[] targetArray, int targetIndex)
        {
            Array.Copy(rawBitmapData, 0, targetArray, targetIndex, rawBitmapData.Length);
        }

        byte[] IKeyBitmapDataAccess.CopyData()
        {
            return (byte[])rawBitmapData?.Clone();
        }
    }
}
