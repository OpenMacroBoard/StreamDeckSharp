using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;

namespace OpenMacroBoard.Examples.Austria;

internal static class Program
{
    private static void Main()
    {
        // Create some color we use later to draw the flag of Austria
        var red = KeyBitmap.Create.FromRgb(237, 41, 57);
        var white = KeyBitmap.Create.FromRgb(255, 255, 255);
        var rowColors = new KeyBitmap[] { red, white, red };

        // Open a 5x3 macro board.
        using var deck = ExampleHelper.OpenBoard(d => d.Keys.CountX == 5 && d.Keys.CountY == 3);

        // Send the bitmap information to the device
        for (var i = 0; i < deck.Keys.Count; i++)
        {
            deck.SetKeyBitmap(i, rowColors[i / 5]);
        }

        ExampleHelper.WaitForKeyToExit();
    }
}
