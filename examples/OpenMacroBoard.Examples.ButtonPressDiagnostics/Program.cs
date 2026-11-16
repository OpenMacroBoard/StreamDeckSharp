using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Globalization;

namespace OpenMacroBoard.Examples.ButtonPressDiagnostics;

internal static class Program
{
    private static readonly Font Font = SystemFonts.CreateFont("Arial", 24);
    private static int[] counter;

    private static void Main(string[] args)
    {
        using var deck = ExampleHelper.OpenBoard();

        deck.SetBrightness(100);
        counter = new int[deck.Keys.Count];

        Console.WriteLine("INFO: Press some keys on the Stream Deck.");
        deck.ClearKeys();
        deck.KeyStateChanged += Deck_KeyPressed;
        Console.WriteLine();

        Console.WriteLine("Options: (q)uit, (c)lear, (r)eset");

        while (true)
        {
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Q:
                    return;

                case ConsoleKey.C:
                    deck.ClearKeys();
                    break;

                case ConsoleKey.R:
                    deck.ClearKeys();
                    deck.SetBrightness(100);
                    Array.Clear(counter, 0, counter.Length);
                    break;
            }
        }
    }

    private static void Deck_KeyPressed(object sender, KeyEventArgs e)
    {
        if (sender is not IMacroBoard d)
        {
            return;
        }

        if (e.IsDown)
        {
            counter[e.Key]++;
        }

        var keyImage = new Image<Bgr24>(96, 96);

        keyImage.Mutate(img =>
        {
            if (e.IsDown)
            {
                var circle = new EllipsePolygon(72, 20, 5);
                img.Fill(Color.White, circle);
            }

            img.DrawText(
                counter[e.Key].ToString(CultureInfo.InvariantCulture),
                Font,
                Color.White,
                new PointF(10, 10)
            );
        });

        var key = KeyBitmap.Create.FromImageSharpImage(keyImage);

        d.SetKeyBitmap(e.Key, key);
    }
}
