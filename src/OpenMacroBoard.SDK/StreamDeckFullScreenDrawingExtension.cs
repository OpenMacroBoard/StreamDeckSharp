using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// Extension method to generate fullscreen images on <see cref="IMacroBoard"/>s.
    /// </summary>
    public static class DrawFullScreenExtension
    {
        private static readonly Brush black = Brushes.Black;

        /// <summary>
        /// Draw a given image as fullscreen (spanning over all keys)
        /// </summary>
        /// <param name="board"></param>
        /// <param name="b"></param>
        public static void DrawFullScreenBitmap(this IMacroBoard board, Bitmap b)
        {
            byte[] imgData = null;

            using (var resizedImage = ResizeToFullStreamDeckImage(b, board.Keys.Area.Size))
            {
                imgData = GetRgbArray(resizedImage);
            }

            for (int i = 0; i < board.Keys.Count; i++)
            {
                var img = GetKeyImageFromFull(board.Keys[i], imgData, board.Keys.Area.Size);
                board.SetKeyBitmap(i, img);
            }
        }

        private static Bitmap ResizeToFullStreamDeckImage(Bitmap b, Size newSize)
        {
            var newBm = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
            double scale = Math.Max((double)newSize.Width / b.Width, (double)newSize.Height / b.Height);

            using (var g = Graphics.FromImage(newBm))
            {
                g.InterpolationMode = InterpolationMode.High;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var scaleWidth = (int)(b.Width * scale);
                var scaleHeight = (int)(b.Height * scale);

                g.FillRectangle(black, new RectangleF(0, 0, newSize.Width, newSize.Height));
                g.DrawImage(b, new Rectangle(((int)newSize.Width - scaleWidth) / 2, ((int)newSize.Height - scaleHeight) / 2, scaleWidth, scaleHeight));
            }

            return newBm;
        }

        static byte[] GetRgbArray(Bitmap b)
        {
            var rect = new Rectangle(0, 0, b.Width, b.Height);
            var lockData = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                var data = new byte[lockData.Stride * b.Height];
                Marshal.Copy(lockData.Scan0, data, 0, data.Length);
                return data;
            }
            finally
            {
                b.UnlockBits(lockData);
            }
        }

        static KeyBitmap GetKeyImageFromFull(Rectangle keyPos, byte[] fullImageData, Size fullImageSize)
        {
            var keyImgData = new byte[keyPos.Width * keyPos.Height * 3];
            var stride = 4 * ((fullImageSize.Width * 3 + 3) / 4);

            for (int y = 0; y < keyPos.Height; y++)
            {
                //var numberOfPixelsInPrevRows = (keyPos.Top + y) * fullImageSize.Width + keyPos.Left;
                for (int x = 0; x < keyPos.Width; x++)
                {
                    //var p = (numberOfPixelsInPrevRows + x) * 3;
                    //var kPos = (y * keyPos.Width + x) * 3;
                    var p = (keyPos.Top + y) * stride + (keyPos.Left + x) * 3;
                    var kPos = (y * keyPos.Width + x) * 3;

                    keyImgData[kPos + 0] = fullImageData[p + 0];
                    keyImgData[kPos + 1] = fullImageData[p + 1];
                    keyImgData[kPos + 2] = fullImageData[p + 2];
                }
            }

            return new KeyBitmap(keyPos.Width, keyPos.Height, keyImgData);
        }
    }
}
