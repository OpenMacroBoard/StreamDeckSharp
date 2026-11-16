using OpenMacroBoard.Examples.CommonStuff;
using System;

namespace OpenMacroBoard.Examples.ImageGlitchTest;

internal static class Program
{
    private static bool exitRequested = false;
    private static bool readGlitchEnabled = true;

    private static void Main()
    {
        Console.CancelKeyPress += Console_CancelKeyPress;

        using var deck = ExampleHelper.OpenBoard();

        var imgFactory = new ReferenceImageFactory(deck.GetDeviceImageSize(), deck.Keys.Count);

        Console.WriteLine("Use numpad + and - to iterate through different patterns and return to toggle ReadGlitch.");
        Console.WriteLine();

        while (!exitRequested)
        {
            for (var i = 0; i < deck.Keys.Count; i++)
            {
                deck.SetKeyBitmap(i, imgFactory.GetKeyBitmap(i));

                if (readGlitchEnabled)
                {
                    deck.GetSerialNumber();
                }
            }

            ProcessConsoleKeys(imgFactory);
        }
    }

    private static void ProcessConsoleKeys(ReferenceImageFactory imgFactory)
    {
        if (Console.KeyAvailable)
        {
            var k = Console.ReadKey(true);

            if (k.Key is ConsoleKey.Add or ConsoleKey.OemPlus)
            {
                imgFactory.CurrentMode++;
            }
            else if (k.Key is ConsoleKey.Subtract or ConsoleKey.OemMinus)
            {
                imgFactory.CurrentMode--;
            }
            else if (k.Key == ConsoleKey.Enter)
            {
                readGlitchEnabled = !readGlitchEnabled;
            }
            else
            {
                Console.WriteLine($"Ignoring unmapped key: {k.Key}");
            }

            Console.WriteLine($"Pattern ID: {imgFactory.CurrentMode,2}{(readGlitchEnabled ? " + GLITCH" : string.Empty)}");
        }
    }

    private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        exitRequested = true;
    }
}
