using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OpenMacroBoard.Examples.DrawKeys;

internal static class Program
{
    private const int KeySize = 100;

    private static readonly Font Regular = SystemFonts.CreateFont("Arial", 20);
    private static readonly Font Large = SystemFonts.CreateFont("Arial", 60, FontStyle.Bold);

    private static readonly PointF RegularOrigin = new(35, 40);
    private static readonly PointF LargeOrigin = new(15, 18);

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

            var image = new Image<Bgr24>(KeySize, KeySize);
            image.Mutate(x => x.DrawText($"{kId,2}", font, Color.White, origin));

            var bmp = KeyBitmap.Create.FromImageSharpImage(image);

            deck.SetKeyBitmap(kId, bmp);
        }
    }
}
