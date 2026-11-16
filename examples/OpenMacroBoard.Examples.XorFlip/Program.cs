using OpenMacroBoard.Examples.CommonStuff;

namespace OpenMacroBoard.Examples.XorFlip;

internal static class Program
{
    private static void Main(string[] args)
    {
        using var deck = ExampleHelper.OpenBoard();

        var host = new MacroBoardHost(deck);

        // The constructor starts the game.
        _ = new XorFlipGame(host);

        ExampleHelper.WaitForKeyToExit();
    }
}
