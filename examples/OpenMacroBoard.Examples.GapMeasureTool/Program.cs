using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SkiaSharp;
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

        var rect = SKRectI.Create(
            secondKeyRect.X,
            secondKeyRect.Y,
            secondKeyRect.Width,
            secondKeyRect.Height
        );

        var keyArea = deck.Keys.Area;

        // no anti-aliasing: the edges of the rectangle have to be sharp to measure with it
        using var white = new SKPaint { Color = SKColors.White, IsAntialias = false };

        while (true)
        {
            var current = rect;

            var fullScreenBmp = KeyBitmap.Create.FromSkiaCanvas(
                keyArea.Width,
                keyArea.Height,
                canvas => canvas.DrawRect(current, white)
            );

            deck.DrawFullScreenBitmap(fullScreenBmp);

            var k = Console.ReadKey(true);

            if (k.Key == ConsoleKey.Q)
            {
                break;
            }

            if (k.Key == ConsoleKey.A)
            {
                rect.Offset(-1, 0);
            }
            else if (k.Key == ConsoleKey.D)
            {
                rect.Offset(1, 0);
            }
            else if (k.Key == ConsoleKey.W)
            {
                rect.Offset(0, -1);
            }
            else if (k.Key == ConsoleKey.S)
            {
                rect.Offset(0, 1);
            }
            else if (k.Key == ConsoleKey.J)
            {
                rect.Right--;
            }
            else if (k.Key == ConsoleKey.L)
            {
                rect.Right++;
            }
            else if (k.Key == ConsoleKey.I)
            {
                rect.Bottom++;
            }
            else if (k.Key == ConsoleKey.K)
            {
                rect.Bottom--;
            }
        }
    }
}
