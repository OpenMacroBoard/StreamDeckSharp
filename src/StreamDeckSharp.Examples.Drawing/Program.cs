using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Controls;

namespace StreamDeckSharp.Examples.Drawing
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //Open the Stream Deck device
            using (var deck = StreamDeck.OpenDevice())
            {
                ConsoleWriteAndWait("Press any key to run System.Drawing example");
                ExampleWithSystemDrawing(deck);

                ConsoleWriteAndWait("Press any key to run WPF FrameworkElement example");
                ExampleWithWpfElement(deck);

                ConsoleWriteAndWait("Press any key to exit");
            }
        }

        static void ExampleWithSystemDrawing(IStreamDeck deck)
        {
            //Create a key with lambda graphics
            var key = KeyBitmap.FromGraphics(g =>
            {
                //See https://stackoverflow.com/questions/6311545/c-sharp-write-text-on-bitmap for details
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                //Fill background black
                g.FillRectangle(Brushes.Black, 0, 0, deck.IconSize, deck.IconSize);

                //Write text to graphics
                var f = new Font("Arial", 13);
                g.DrawString("Drawing", f, Brushes.White, new PointF(5, 20));

                //Draw other stuff to image
                //g.DrawImage
                //g.DrawLine
            });

            deck.SetKeyBitmap(7, key);
        }

        static void ExampleWithWpfElement(IStreamDeck deck)
        {
            var c = new Canvas();
            c.Width = deck.IconSize;
            c.Height = deck.IconSize;
            c.Background = System.Windows.Media.Brushes.Black;

            var t = new TextBlock();
            t.Text = "WPF";
            t.FontFamily = new System.Windows.Media.FontFamily("Arial");
            t.FontSize = 13;
            t.Foreground = System.Windows.Media.Brushes.White;

            Canvas.SetLeft(t, 10);
            Canvas.SetTop(t, 10);

            c.Children.Add(t);

            var k = KeyBitmap.FromWpfElement(c);
            deck.SetKeyBitmap(7, k);
        }

        static void ConsoleWriteAndWait(string text)
        {
            Console.WriteLine(text);
            Console.ReadKey();
        }
    }
}
