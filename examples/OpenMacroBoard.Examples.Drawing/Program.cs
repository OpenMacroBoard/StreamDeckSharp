using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SkiaSharp;
using System;

namespace OpenMacroBoard.Examples.Drawing;

internal static class Program
{
    private const int KeySize = 100;

    [STAThread]
    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();

        ExampleWithSkiaSharp(deck);
        ExampleHelper.WaitForKeyToExit();
    }

    private static void ExampleWithSkiaSharp(IMacroBoard deck)
    {
        using var font = ExampleFonts.Create(13);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        // The canvas starts black. SkiaSharp places text by its baseline (here at y = 32).
        var key = KeyBitmap.Create.FromSkiaCanvas(
            KeySize,
            KeySize,
            canvas => canvas.DrawText("Your Text", 5, 32, SKTextAlign.Left, font, paint)
        );

        deck.SetKeyBitmap(key);
    }
}
