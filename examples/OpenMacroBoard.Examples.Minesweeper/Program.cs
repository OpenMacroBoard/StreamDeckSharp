using OpenMacroBoard.Examples.CommonStuff;

namespace OpenMacroBoard.Examples.Minesweeper;

internal static class Program
{
    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();
        using var wrapper = new MinesweeperDeckWrapper(deck, new ClassicMinesweeperIconSet(), 300);

        ExampleHelper.WaitForKeyToExit();
    }
}
