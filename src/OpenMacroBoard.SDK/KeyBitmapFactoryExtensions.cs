using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// KeyBitmap factory extensions based on System.Windows (WPF)
    /// </summary>
    public static class KeyBitmapFactoryExtensions
    {
        /// <summary>
        /// Uses a WPF FrameworkElement to create a keyImage
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static KeyBitmap FromWpfElement(
            this IKeyBitmapFactory builder,
            int width,
            int height,
            FrameworkElement e
        )
        {
            //Do WPF layout process manually (because the element is not a UI element)
            e.Measure(new Size(width, height));
            e.Arrange(new Rect(0, 0, width, height));
            e.UpdateLayout();

            //Render the element as bitmap
            RenderTargetBitmap renderer = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderer.Render(e);

            //Convert to StreamDeck compatible format
            var pbgra32 = new byte[width * height * 4];
            renderer.CopyPixels(pbgra32, width * height * 4, 0);

            var bitmapData = ConvertPbgra32ToBgr24(pbgra32, width, height);
            return new KeyBitmap(width, height, bitmapData);
        }

        /// <summary>
        /// Convert 32bit color (4 channel) to 24bit bgr
        /// </summary>
        /// <param name="pbgra32"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal static byte[] ConvertPbgra32ToBgr24(byte[] pbgra32, int width, int height)
        {
            var data = new byte[width * height * 3];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var pos = y * width + x;
                    var posSrc = pos * 4;
                    var posTar = pos * 3;

                    data[pos + 0] = pbgra32[pos + 0];
                    data[pos + 1] = pbgra32[pos + 1];
                    data[pos + 2] = pbgra32[pos + 2];
                    data[pos + 3] = pbgra32[pos + 3];
                }

            return data;
        }
    }
}
