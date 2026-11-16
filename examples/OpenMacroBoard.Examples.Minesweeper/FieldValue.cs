namespace OpenMacroBoard.Examples.Minesweeper;

internal class FieldValue
{
    public bool IsVisible { get; set; }
    public bool IsMine { get; set; }
    public bool IsMarkedWithFlag { get; set; }
    public int NeighbourMineCount { get; set; }
}
