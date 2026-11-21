using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using System;

namespace OpenMacroBoard.Examples.Rainbow;

internal static class Program
{
    private static readonly Random Rnd = new();
    private static readonly byte[] RgbBuffer = new byte[3];

    private static readonly int FirstKeyId = 0;
    private static int lastKeyId = 0;

    private static bool firstKeyPressed = false;
    private static bool lastKeyPressed = false;

    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();

        lastKeyId = deck.Keys.Count - 1;

        Console.WriteLine("INFO: Press some keys on the Stream Deck.");

        deck.ClearKeys();
        deck.KeyStateChanged += Deck_KeyPressed;
        Console.WriteLine();

        ExampleHelper.WaitForKeyToExit();
    }

    private static void Deck_KeyPressed(object sender, KeyEventArgs e)
    {
        if (sender is not IMacroBoard d)
        {
            return;
        }

        if (e.IsDown)
        {
            d.SetKeyBitmap(e.Key, GetRandomColorImage());
        }

        if (e.Key == FirstKeyId)
        {
            firstKeyPressed = e.IsDown;
        }

        if (e.Key == lastKeyId)
        {
            lastKeyPressed = e.IsDown;
        }

        if (firstKeyPressed && lastKeyPressed)
        {
            d.ClearKeys();
        }
    }

    private static KeyBitmap GetRandomColorImage()
    {
        Rnd.NextBytes(RgbBuffer);
        return KeyBitmap.Create.FromRgb(RgbBuffer[0], RgbBuffer[1], RgbBuffer[2]);
    }
}
