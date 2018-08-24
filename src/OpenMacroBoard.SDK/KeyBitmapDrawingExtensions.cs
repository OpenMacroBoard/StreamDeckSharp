using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// KeyBitmap factory extensions based on System.Drawing
    /// </summary>
    public static class KeyBitmapDrawingExtensions
    {
        /// <summary>
        /// Create a bitmap from encoded image stream
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="bitmapStream"></param>
        /// <returns></returns>
        public static KeyBitmap FromStream(this IKeyBitmapFactory builder, Stream bitmapStream)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromStream(bitmapStream))
                return builder.FromBitmap(bitmap);
        }

        /// <summary>
        /// Create a bitmap from encoded image
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="bitmapFile"></param>
        /// <returns></returns>
        public static KeyBitmap FromFile(this IKeyBitmapFactory builder, string bitmapFile)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromFile(bitmapFile))
                return builder.FromBitmap(bitmap);
        }

        /// <summary>
        /// Create key bitmap from graphics commands (for example with lambda expression)
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="graphicsAction"></param>
        /// <returns></returns>
        public static KeyBitmap FromGraphics(
            this IKeyBitmapFactory builder,
            int width,
            int height,
            Action<Graphics> graphicsAction
        )
        {
            using (var bmp = CreateKeyBitmap(width, height))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                    graphicsAction(g);

                return builder.FromBitmap(bmp);
            }
        }

        /// <summary>
        /// Creates a <see cref="KeyBitmap"/> from a given <see cref="Bitmap"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static KeyBitmap FromBitmap(this IKeyBitmapFactory builder, Bitmap bitmap)
        {
            var w = bitmap.Width;
            var h = bitmap.Height;

            BitmapData data = null;
            try
            {
                data = bitmap.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var managedBGR = new byte[w * h * 3];

                unsafe
                {
                    byte* bdata = (byte*)data.Scan0;

                    if (data.PixelFormat == PixelFormat.Format24bppRgb)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                var ps = data.Stride * y + x * 3;
                                var pt = (w * y + x) * 3;
                                managedBGR[pt + 0] = bdata[ps + 0];
                                managedBGR[pt + 1] = bdata[ps + 1];
                                managedBGR[pt + 2] = bdata[ps + 2];
                            }
                        }
                    }
                    else if (data.PixelFormat == PixelFormat.Format32bppArgb)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                var ps = data.Stride * y + x * 4;
                                var pt = (w * y + x) * 3;
                                double alpha = (double)bdata[ps + 3] / 255f;
                                managedBGR[pt + 0] = (byte)Math.Round(bdata[ps + 0] * alpha);
                                managedBGR[pt + 1] = (byte)Math.Round(bdata[ps + 1] * alpha);
                                managedBGR[pt + 2] = (byte)Math.Round(bdata[ps + 2] * alpha);
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported pixel format");
                    }
                }

                return new KeyBitmap(w, h, managedBGR);
            }
            finally
            {
                if (data != null)
                    bitmap.UnlockBits(data);
            }
        }

        private static Bitmap CreateKeyBitmap(int width, int height)
        {
            var img = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            return img;
        }
    }
}
