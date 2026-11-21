using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SkiaSharp;
using System;
using System.Globalization;

namespace OpenMacroBoard.Examples.ButtonPressDiagnostics;

internal static class Program
{
    private static readonly SKFont Font = ExampleFonts.Create(24);
    private static readonly SKPaint White = new() { Color = SKColors.White, IsAntialias = true };
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

        var count = counter[e.Key].ToString(CultureInfo.InvariantCulture);

        var key = KeyBitmap.Create.FromSkiaCanvas(96, 96, canvas =>
        {
            if (e.IsDown)
            {
                canvas.DrawCircle(72, 20, 5, White);
            }

            // SkiaSharp places text by its baseline
            canvas.DrawText(count, 10, 32, SKTextAlign.Left, Font, White);
        });

        d.SetKeyBitmap(e.Key, key);
    }
}
