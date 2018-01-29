using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace StreamDeckSharp.Examples.DrawFullScreen
{
    class Program
    {
        static void Main(string[] args)
        {
            var testImg = @"C:\testimage.png";

            using (var deck = StreamDeck.FromHID())
            using (var bmp = (Bitmap)Bitmap.FromFile(testImg))
            {
                deck.DrawFullScreenBitmap(bmp);
                Console.ReadKey();
            }
        }
    }
}
