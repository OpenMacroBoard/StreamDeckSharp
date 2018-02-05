using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StreamDeckSharp
{
    public partial class KeyBitmap
    {
        /// <summary>
        /// Uses a WPF FrameworkElement to create a keyImage
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static KeyBitmap FromWpfElement(FrameworkElement e)
        {
            //Do WPF layout process manually (because the element is not a UI element)
            e.Measure(new Size(72, 72));
            e.Arrange(new Rect(0, 0, 72, 72));
            e.UpdateLayout();

            //Render the element as bitmap
            RenderTargetBitmap renderer = new RenderTargetBitmap(72, 72, 96, 96, PixelFormats.Pbgra32);
            renderer.Render(e);

            //Convert to StreamDeck compatible format
            var pbgra32 = new byte[72 * 72 * 4];
            renderer.CopyPixels(pbgra32, 72 * 4, 0);

            var bitmapData = ConvertPbgra32ToStreamDeckKey(pbgra32);
            return new KeyBitmap(bitmapData);
        }

        /// <summary>
        /// Convert 32bit color (4 channel) to 24bit bgr + mirror lines horizontally (for streamdeck)
        /// </summary>
        /// <param name="pbgra32"></param>
        /// <returns></returns>
        private static byte[] ConvertPbgra32ToStreamDeckKey(byte[] pbgra32)
        {
            var data = new byte[72 * 72 * 3];
            for (int y = 0; y < 72; y++)
                for (int x = 0; x < 72; x++)
                    for (int c = 0; c < 3; c++)
                        data[3 * (y * 72 + (71 - x)) + c] = pbgra32[4 * (y * 72 + x) + c];
            return data;
        }
    }
}
