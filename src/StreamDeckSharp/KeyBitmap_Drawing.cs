using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace StreamDeckSharp
{
    public partial class KeyBitmap
    {
        /// <summary>
        /// Creates a solid color bitmap
        /// </summary>
        /// <param name="R">Red channel</param>
        /// <param name="G">Green channel</param>
        /// <param name="B">Blue channel</param>
        /// <returns></returns>
        public static KeyBitmap FromRGBColor(byte R, byte G, byte B)
        {
            //If everything is 0 (black) take a shortcut ;-)
            if (R == 0 && G == 0 && B == 0) return Black;

            var buffer = new byte[HidClient.rawBitmapDataLength];
            for (int i = 0; i < buffer.Length; i += 3)
            {
                buffer[i + 0] = B;
                buffer[i + 1] = G;
                buffer[i + 2] = R;
            }

            return new KeyBitmap(buffer);
        }

        /// <summary>
        /// Create a bitmap from encoded image stream
        /// </summary>
        /// <param name="bitmapStream"></param>
        /// <returns></returns>
        public static KeyBitmap FromStream(Stream bitmapStream)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromStream(bitmapStream))
            {
                return FromDrawingBitmap(bitmap);
            }
        }

        /// <summary>
        /// Create a bitmap from encoded image
        /// </summary>
        /// <param name="bitmapFile"></param>
        /// <returns></returns>
        public static KeyBitmap FromFile(string bitmapFile)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromFile(bitmapFile))
            {
                return FromDrawingBitmap(bitmap);
            }
        }

        /// <summary>
        /// Create key bitmap from graphics commands (for example with lambda expression)
        /// </summary>
        /// <param name="graphicsAction"></param>
        /// <returns></returns>
        public static KeyBitmap FromGraphics(Action<Graphics> graphicsAction)
        {
            using (var bmp = CreateKeyBitmap(HidClient.iconSize))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    graphicsAction(g);
                }

                var bitmapData = GetStreamDeckDataFromBitmap(HidClient.iconSize, bmp);
                return new KeyBitmap(bitmapData);
            }
        }

        /// <summary>
        /// Create bitmap from 72x72 3x 8 bit channel BGR
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <returns></returns>
        public static KeyBitmap FromRawBitmap(byte[] bitmapData)
        {
            var c = (byte[])bitmapData.Clone();
            FlipHorizontal(c, HidClient.iconSize);
            return new KeyBitmap(c);
        }

        private static byte[] GetStreamDeckDataFromBitmap(int iconSize, Bitmap image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (image.Width != iconSize || image.Height != iconSize) throw new ArgumentException("Bitmap size not supported");
            if (image.PixelFormat != PixelFormat.Format24bppRgb) throw new BadImageFormatException("Format is not supported");

            var lockData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                var rgbValues = new byte[iconSize * iconSize * 3];
                Marshal.Copy(lockData.Scan0, rgbValues, 0, rgbValues.Length);
                FlipHorizontal(rgbValues, HidClient.iconSize);
                return rgbValues;
            }
            finally
            {
                image.UnlockBits(lockData);
            }
        }

        private static KeyBitmap FromDrawingBitmap(Bitmap bitmap)
        {
            if (bitmap.Width != HidClient.iconSize || bitmap.Height != HidClient.iconSize) throw new NotSupportedException("Unsupported bitmap dimensions");

            BitmapData data = null;
            try
            {
                data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var managedRGB = new byte[HidClient.rawBitmapDataLength];

                unsafe
                {
                    byte* bdata = (byte*)data.Scan0;

                    //TODO: This should be cleaned up
                    //I'm locking for a different approach to parse different PixelFormats without
                    //copying 90% of the code ;-)
                    if (data.PixelFormat == PixelFormat.Format24bppRgb)
                    {
                        for (int y = 0; y < HidClient.iconSize; y++)
                        {
                            for (int x = 0; x < HidClient.iconSize; x++)
                            {
                                var ps = data.Stride * y + x * 3;
                                var pt = HidClient.iconSize * 3 * (y + 1) - (x + 1) * 3;
                                managedRGB[pt + 0] = bdata[ps + 0];
                                managedRGB[pt + 1] = bdata[ps + 1];
                                managedRGB[pt + 2] = bdata[ps + 2];
                            }
                        }
                    }
                    else if (data.PixelFormat == PixelFormat.Format32bppArgb)
                    {
                        for (int y = 0; y < HidClient.iconSize; y++)
                        {
                            for (int x = 0; x < HidClient.iconSize; x++)
                            {
                                var ps = data.Stride * y + x * 4;
                                var pt = HidClient.iconSize * 3 * (y + 1) - (x + 1) * 3;
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

                return new KeyBitmap(managedRGB);
            }
            finally
            {
                if (data != null)
                    bitmap.UnlockBits(data);
            }
        }

        private static void FlipHorizontal(byte[] data, int size)
        {
            var hsize = size / 2;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < hsize; x++)
                    for (int c = 0; c < 3; c++)
                    {
                        var posA = 3 * (y * 72 + (71 - x)) + c;
                        var posB = 3 * (y * 72 + x) + c;

                        var tmp = data[posA];
                        data[posA] = data[posB];
                        data[posB] = tmp;
                    }

        }

        private static Bitmap CreateKeyBitmap(int iconSize)
        {
            var img = new Bitmap(iconSize, iconSize, PixelFormat.Format24bppRgb);
            return img;
        }
    }
}
