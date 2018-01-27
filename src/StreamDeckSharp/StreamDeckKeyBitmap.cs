using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StreamDeckSharp
{
    /// <summary>
    /// Represents a bitmap that can be used as key images
    /// </summary>
    public partial class StreamDeckKeyBitmap
    {
        /// <summary>
        /// Solid black bitmap
        /// </summary>
        /// <remarks>
        /// If you need a black bitmap (for example to clear keys) use this property for better performance (in theory ^^)
        /// </remarks>
        public static StreamDeckKeyBitmap Black { get { return black; } }
        private static readonly StreamDeckKeyBitmap black = new StreamDeckKeyBitmap(null);

        /// <remarks>
        /// The raw pixel format is a byte array of length 15552. This number is based on the image
        /// dimensions used by StreamDeck 72x72 pixels and 3 channels (RGB) for each pixel. 72 x 72 x 3 = 15552.
        /// 
        /// The channels are in the order BGR and the pixel rows (stride) are in reverse order.
        /// If you need some help try <see cref="StreamDeckKeyBitmap"/>
        /// </remarks>
        internal readonly byte[] rawBitmapData;

        internal StreamDeckKeyBitmap(byte[] bitmapData)
        {
            if (bitmapData != null)
            {
                if (bitmapData.Length != StreamDeckHID.rawBitmapDataLength) throw new NotSupportedException("Unsupported bitmap array length");
                this.rawBitmapData = bitmapData;
            }
        }
    }
}
