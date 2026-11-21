using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SkiaSharp;

namespace OpenMacroBoard.Examples.DrawKeys;

internal static class Program
{
    private const int KeySize = 100;

    private static readonly SKFont Regular = ExampleFonts.Create(20);
    private static readonly SKFont Large = ExampleFonts.Create(60, bold: true);
    private static readonly SKPaint White = new() { Color = SKColors.White, IsAntialias = true };

    /// <summary>
    /// SkiaSharp places text by its baseline, this is the left end of it.
    /// </summary>
    private static readonly SKPoint RegularOrigin = new(35, 58);
    private static readonly SKPoint LargeOrigin = new(15, 72);

    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();

        var lastActive = -1;
        deck.KeyStateChanged += (_, e) =>
        {
            if (!e.IsDown)
            {
                return;
            }

            var newId = e.Key;

            if (lastActive == newId)
            {
                newId = -1;
            }

            DrawKeys(deck, newId);
            lastActive = newId;
        };

        DrawKeys(deck);
        ExampleHelper.WaitForKeyToExit();
    }

    private static void DrawKeys(IMacroBoard deck, int activeKey = -1)
    {
        for (var kId = 0; kId < deck.Keys.Count; kId++)
        {
            var font = Regular;
            var origin = RegularOrigin;

            if (kId == activeKey)
            {
                font = Large;
                origin = LargeOrigin;
            }

            var text = $"{kId,2}";

            var bmp = KeyBitmap.Create.FromSkiaCanvas(
                KeySize,
                KeySize,
                canvas => canvas.DrawText(text, origin.X, origin.Y, SKTextAlign.Left, font, White)
            );

            deck.SetKeyBitmap(kId, bmp);
        }
    }
}
