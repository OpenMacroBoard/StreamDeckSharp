using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace OpenMacroBoard.Examples.GapMeasureTool;

internal static class Program
{
    private static void Main()
    {
        // Calibration tip: A rectangle spanning over a single gap should be the same length
        // when moved to span two gaps. If it's not tweak the gap value inside the HardwareInfo classes.

        using var deck = ExampleHelper.OpenBoard();

        Console.WriteLine("Use WASD and IJKL to move and resize the rectangle and press Q to quit.");
        Console.WriteLine();

        var secondKeyRect = deck.Keys[deck.Keys.Count / 2];

        var rect = new Rectangle(
            secondKeyRect.X,
            secondKeyRect.Y,
            secondKeyRect.Width,
            secondKeyRect.Height
        );

        var keyArea = deck.Keys.Area;

        var fullScreenBmp = new Image<Bgr24>(keyArea.Width, keyArea.Height);

        while (true)
        {
            fullScreenBmp.Mutate(x =>
            {
                x.Fill(Color.Black);
                x.Fill(Color.White, rect);
            });

            deck.DrawFullScreenBitmap(fullScreenBmp);

            var k = Console.ReadKey(true);

            if (k.Key == ConsoleKey.Q)
            {
                break;
            }

            if (k.Key == ConsoleKey.A)
            {
                rect.X--;
            }
            else if (k.Key == ConsoleKey.D)
            {
                rect.X++;
            }
            else if (k.Key == ConsoleKey.W)
            {
                rect.Y--;
            }
            else if (k.Key == ConsoleKey.S)
            {
                rect.Y++;
            }
            else if (k.Key == ConsoleKey.J)
            {
                rect.Width--;
            }
            else if (k.Key == ConsoleKey.L)
            {
                rect.Width++;
            }
            else if (k.Key == ConsoleKey.I)
            {
                rect.Height++;
            }
            else if (k.Key == ConsoleKey.K)
            {
                rect.Height--;
            }
        }
    }
}
