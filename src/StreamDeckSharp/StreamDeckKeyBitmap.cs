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
    /// Represents a bitmap that can be used as key images
    /// </summary>
    public class StreamDeckKeyBitmap
    {
        /// <summary>
        /// Solid black bitmap
        /// </summary>
        /// <remarks>
        /// If you need a black bitmap (for example to clear keys) use this property for better performance (in theory ^^)
        /// </remarks>
        public static StreamDeckKeyBitmap Black { get { return black; } }
        private static readonly StreamDeckKeyBitmap black = new StreamDeckKeyBitmap(null);

        internal readonly byte[] rawBitmapData;

        /// <summary>
        /// Returns a copy of the internal bitmap.
        /// </summary>
        /// <returns></returns>
        public byte[] CloneBitmapData()
        {
            return (byte[])rawBitmapData.Clone();
        }

        internal StreamDeckKeyBitmap(byte[] bitmapData)
        {
            if (bitmapData != null)
            {
                if (bitmapData.Length != StreamDeckHID.rawBitmapDataLength) throw new NotSupportedException("Unsupported bitmap array length");
                this.rawBitmapData = bitmapData;
            }
        }

        public static StreamDeckKeyBitmap FromRawBitmap(byte[] bitmapData)
        {
            return new StreamDeckKeyBitmap(bitmapData);
        }

        /// <summary>
        /// Creates a solid color bitmap
        /// </summary>
        /// <param name="R">Red channel</param>
        /// <param name="G">Green channel</param>
        /// <param name="B">Blue channel</param>
        /// <returns></returns>
        public static StreamDeckKeyBitmap FromRGBColor(byte R, byte G, byte B)
        {
            //If everything is 0 (black) take a shortcut ;-)
            if (R == 0 && G == 0 && B == 0) return Black;

            var buffer = new byte[StreamDeckHID.rawBitmapDataLength];
            for (int i = 0; i < buffer.Length; i += 3)
            {
                buffer[i + 0] = B;
                buffer[i + 1] = G;
                buffer[i + 2] = R;
            }

            return new StreamDeckKeyBitmap(buffer);
        }

        public static StreamDeckKeyBitmap FromStream(Stream bitmapStream)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromStream(bitmapStream))
            {
                return FromDrawingBitmap(bitmap);
            }
        }

        public static StreamDeckKeyBitmap FromFile(string bitmapFile)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromFile(bitmapFile))
            {
                return FromDrawingBitmap(bitmap);
            }
        }

        internal static StreamDeckKeyBitmap FromDrawingBitmap(Bitmap bitmap)
        {
            if (bitmap.Width != StreamDeckHID.iconSize || bitmap.Height != StreamDeckHID.iconSize) throw new NotSupportedException("Unsupported bitmap dimensions");

            BitmapData data = null;
            try
            {
                data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var managedRGB = new byte[StreamDeckHID.rawBitmapDataLength];

                unsafe
                {
                    byte* bdata = (byte*)data.Scan0;

                    //TODO: This should be cleaned up
                    //I'm locking for a different approach to parse different PixelFormats without
                    //copying 90% of the code ;-)
                    if (data.PixelFormat == PixelFormat.Format24bppRgb)
                    {
                        for (int y = 0; y < StreamDeckHID.iconSize; y++)
                        {
                            for (int x = 0; x < StreamDeckHID.iconSize; x++)
                            {
                                var ps = data.Stride * y + x * 3;
                                var pt = StreamDeckHID.iconSize * 3 * (y + 1) - (x + 1) * 3;
                                managedRGB[pt + 0] = bdata[ps + 0];
                                managedRGB[pt + 1] = bdata[ps + 1];
                                managedRGB[pt + 2] = bdata[ps + 2];
                            }
                        }
                    }
                    else if (data.PixelFormat == PixelFormat.Format32bppArgb)
                    {
                        for (int y = 0; y < StreamDeckHID.iconSize; y++)
                        {
                            for (int x = 0; x < StreamDeckHID.iconSize; x++)
                            {
                                var ps = data.Stride * y + x * 4;
                                var pt = StreamDeckHID.iconSize * 3 * (y + 1) - (x + 1) * 3;
                                double alpha = (double)bdata[ps + 3] / 255f;
                                managedRGB[pt + 0] = (byte)Math.Round(bdata[ps + 0] * alpha);
                                managedRGB[pt + 1] = (byte)Math.Round(bdata[ps + 1] * alpha);
                                managedRGB[pt + 2] = (byte)Math.Round(bdata[ps + 2] * alpha);
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported pixel format");
                    }
                }

                return new StreamDeckKeyBitmap(managedRGB);
            }
            finally
            {
                if (data != null)
                    bitmap.UnlockBits(data);
            }
        }
    }
}
