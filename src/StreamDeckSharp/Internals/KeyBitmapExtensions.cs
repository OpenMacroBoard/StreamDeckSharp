using OpenMacroBoard.SDK;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace StreamDeckSharp.Internals
{
    internal static class KeyBitmapExtensions
    {
        public static byte[] GetScaledVersion(this KeyBitmap keyBitmap, int width, int height)
        {
            IKeyBitmapDataAccess keyDataAccess = keyBitmap;

            if (keyDataAccess.IsNull)
            {
                return null;
            }

            var rawData = new byte[keyDataAccess.DataLength];
            keyDataAccess.CopyData(rawData, 0);

            if (keyBitmap.Width == width && keyBitmap.Height == height)
            {
                return rawData;
            }

            var destRect = new Rectangle(0, 0, width, height);

            using (var scaledBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            using (var sourceBmp = new Bitmap(keyBitmap.Width, keyBitmap.Height, PixelFormat.Format24bppRgb))
            {
                var bmpData = sourceBmp.LockBits(new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                try
                {
                    Marshal.Copy(rawData, 0, bmpData.Scan0, rawData.Length);
                }
                finally
                {
                    sourceBmp.UnlockBits(bmpData);
                }

                using (var g = System.Drawing.Graphics.FromImage(scaledBmp))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(sourceBmp, destRect, 0, 0, sourceBmp.Width, sourceBmp.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }

                bmpData = scaledBmp.LockBits(destRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    var dataArray = new byte[width * height * 3];
                    Marshal.Copy(bmpData.Scan0, dataArray, 0, dataArray.Length);
                    return dataArray;
                }
                finally
                {
                    scaledBmp.UnlockBits(bmpData);
                }
            }
        }
    }
}
