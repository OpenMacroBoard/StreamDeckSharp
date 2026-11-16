using OpenMacroBoard.SDK;

namespace OpenMacroBoard.Examples.Minesweeper;

internal interface IMinesweeperIconSet
{
    KeyBitmap ExplodedMine { get; }
    KeyBitmap DefusedMine { get; }
    KeyBitmap FlagWithoutMine { get; }
    KeyBitmap HiddenCell { get; }
    KeyBitmap Flag { get; }
    KeyBitmap this[int number] { get; }
}
