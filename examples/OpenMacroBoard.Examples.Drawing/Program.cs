using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Globalization;

namespace OpenMacroBoard.Examples.Drawing;

internal static class Program
{
    private const int KeySize = 100;

    [STAThread]
    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();

        ExampleWithSystemDrawing(deck);
        ExampleHelper.WaitForKeyToExit();
    }

    private static void ExampleWithSystemDrawing(IMacroBoard deck)
    {
        var image = new Image<Bgr24>(KeySize, KeySize);
        var font = SystemFonts.Get("Arial", CultureInfo.InvariantCulture).CreateFont(13);

        image.Mutate(x => x.DrawText("Your Text", font, Color.White, new PointF(5, 20)));

        var key = KeyBitmap.Create.FromImageSharpImage(image);

        deck.SetKeyBitmap(key);
    }
}
