using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using System;
using System.Diagnostics;

namespace OpenMacroBoard.Examples.MeasureSetBitmapSpeed;

internal static class Program
{
    private static void Main()
    {
        const long cnt = 50_000;

        using var deck = ExampleHelper.OpenBoard();

        var sw = Stopwatch.StartNew();
        var imgSize = deck.GetDeviceImageSize();

        // Create random noise image
        var rnd = new Random();
        var raw = new byte[imgSize * imgSize * 3];
        rnd.NextBytes(raw);
        var rndImage = KeyBitmap.Create.FromBgr24Array(imgSize, imgSize, raw);

        deck.ClearKeys();

        // Run a few SetKeyBitmaps
        var i = cnt;

        while (--i > 0)
        {
            deck.SetKeyBitmap(0, rndImage);
        }

        var t = sw.Elapsed.TotalSeconds;
        var setKeyTime = t / cnt;

        // about 0.5µs on my machine
        Console.WriteLine((setKeyTime * 1000000.0) + " µs");

        // Make sure that the test takes a least 3 seconds on your machine
        // -> change cnt!
        Console.WriteLine("Total Test time: " + t + " s");

        Console.ReadKey();
    }
}
