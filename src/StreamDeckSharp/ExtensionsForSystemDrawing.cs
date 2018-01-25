using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckSharp
{
    public static class ExtensionsForSystemDrawing
    {
        public static byte[] CreateKeyFromGraphics(this IStreamDeck deck, Action<Graphics> graphicsAction)
        {
            using (var bmp = deck.CreateKeyBitmap())
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    graphicsAction(g);
                }
                return deck.GetStreamDeckDataFromBitmap(bmp);
            }
        }

        private static byte[] GetStreamDeckDataFromBitmap(this IStreamDeck deck, Bitmap image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (image.Width != deck.IconSize || image.Height != deck.IconSize) throw new ArgumentException("Bitmap size not supported");
            if (image.PixelFormat != PixelFormat.Format24bppRgb) throw new BadImageFormatException("Format is not supported");

            var lockData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                var rgbValues = new byte[deck.IconSize * deck.IconSize * 3];
                Marshal.Copy(lockData.Scan0, rgbValues, 0, rgbValues.Length);
                FlipHorizontal(rgbValues, 72);
                return rgbValues;
            }
            finally
            {
                image.UnlockBits(lockData);
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

        private static Bitmap CreateKeyBitmap(this IStreamDeck deck)
        {
            var img = new Bitmap(72, 72, PixelFormat.Format24bppRgb);
            return img;
        }
    }
}
