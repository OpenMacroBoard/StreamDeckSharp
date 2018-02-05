using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace StreamDeckSharp.Examples.DrawFullScreen
{
    public static class StreamDeckFullScreenDrawingExtension
    {
        private const int buttonPxSize = 72;
        private const int buttonPxDist = 25; //measured
        private const int fullPxWidth = 5 * buttonPxSize + 4 * buttonPxDist;
        private const int fullPxHeight = 3 * buttonPxSize + 2 * buttonPxDist;
        private static readonly Brush black = Brushes.Black;

        public static void DrawFullScreenBitmap(this IStreamDeck deck, Bitmap b)
        {
            byte[] imgData = null;
            using (var resizedImage = ResizeToFullStreamDeckImage(b))
            {
                imgData = GetRgbArray(resizedImage);
            }

            for (int i = 0; i < deck.KeyCount; i++)
            {
                var img = GetKeyImageFromFull(i, imgData);
                deck.SetKeyBitmap(i, img);
            }
        }
        
        private static Bitmap ResizeToFullStreamDeckImage(Bitmap b)
        {
            var newBm = new Bitmap(fullPxWidth, fullPxHeight, PixelFormat.Format24bppRgb);
            double scale = Math.Min((double)fullPxWidth / b.Width, (double)fullPxHeight / b.Height);

            using (var g = Graphics.FromImage(newBm))
            {
                g.InterpolationMode = InterpolationMode.High;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var scaleWidth = (int)(b.Width * scale);
                var scaleHeight = (int)(b.Height * scale);

                g.FillRectangle(black, new RectangleF(0, 0, fullPxWidth, fullPxHeight));
                g.DrawImage(b, new Rectangle(((int)fullPxWidth - scaleWidth) / 2, ((int)fullPxHeight - scaleHeight) / 2, scaleWidth, scaleHeight));
            }

            return newBm;
        }

        static byte[] GetRgbArray(Bitmap b)
        {
            var rect = new Rectangle(0, 0, b.Width, b.Height);
            var lockData = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                var data = new byte[fullPxWidth * fullPxHeight * 3];
                Marshal.Copy(lockData.Scan0, data, 0, data.Length);
                return data;
            }
            finally
            {
                b.UnlockBits(lockData);
            }
        }

        static KeyBitmap GetKeyImageFromFull(int keyId, byte[] fullImageData)
        {
            var y = keyId / 5;
            var x = 4 - keyId % 5;
            return GetKeyImageFromFull(x, y, fullImageData);
        }

        static KeyBitmap GetKeyImageFromFull(int xPos, int yPos, byte[] fullImageData)
        {
            var keyImgData = new byte[buttonPxSize * buttonPxSize * 3];
            var xOffset = xPos * (buttonPxSize + buttonPxDist);
            var yOffset = yPos * (buttonPxSize + buttonPxDist);

            for (int y = 0; y < buttonPxSize; y++)
            {
                var numberOfPixelsInPrevRows = (y + yOffset) * fullPxWidth + xOffset;
                for (int x = 0; x < buttonPxSize; x++)
                {
                    var p = (numberOfPixelsInPrevRows + x) * 3;
                    var kPos = (y * buttonPxSize + x) * 3;
                    keyImgData[kPos + 0] = fullImageData[p + 0];
                    keyImgData[kPos + 1] = fullImageData[p + 1];
                    keyImgData[kPos + 2] = fullImageData[p + 2];
                }
            }

            return KeyBitmap.FromRawBitmap(keyImgData);
        }
    }
}
