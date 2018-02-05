using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckSharp.Examples.Rainbow
{
    class Program
    {
        private static readonly Random rnd = new Random();
        private static readonly ManualResetEvent exitSignal = new ManualResetEvent(false);
        private static byte[] rgbBuffer = new byte[3];

        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
         
            using (var deck = StreamDeck.OpenDevice())
            {
                deck.SetBrightness(100);

                Console.WriteLine("Connected. Now press some keys on the Stream Deck.");
                deck.ClearKeys();
                deck.KeyStateChanged += Deck_KeyPressed;

                Console.WriteLine("To close the console app press Ctrl + C");
                exitSignal.WaitOne();
            }
        }

        private static void Deck_KeyPressed(object sender, KeyEventArgs e)
        {
            var d = sender as IStreamDeck;
            if (d == null) return;

            if (e.IsDown)
                d.SetKeyBitmap(e.Key, GetRandomColorImage());
        }

        private static KeyBitmap GetRandomColorImage()
        {
            rnd.NextBytes(rgbBuffer);
            return KeyBitmap.FromRGBColor(rgbBuffer[0], rgbBuffer[1], rgbBuffer[2]);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            exitSignal.Set();
            e.Cancel = true;
        }
    }
}
