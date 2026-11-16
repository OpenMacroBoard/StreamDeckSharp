using System.Collections.Generic;

namespace OpenMacroBoard.Examples.Minesweeper;

internal class MinesweeperGame
{
    private readonly FieldValue[,] field;

    private int fieldsToOpen;

    public MinesweeperGame(int width, int height)
    {
        Width = width;
        Height = height;
        field = new FieldValue[width, height];
        fieldsToOpen = width * height;
        InitializeField();
    }

    public int Width { get; }
    public int Height { get; }
    public bool GameOver => GameState != MinesweeperGameState.Running;
    public MinesweeperGameState GameState { get; private set; } = MinesweeperGameState.Running;

    public FieldValue this[int x, int y] => field[x, y];

    public void ToggleFlag(int x, int y)
    {
        if (GameOver)
        {
            return;
        }

        var cell = this[x, y];

        if (cell.IsVisible)
        {
            return;
        }

        cell.IsMarkedWithFlag = !cell.IsMarkedWithFlag;
    }

    public void OpenField(int x, int y)
    {
        if (GameOver)
        {
            return;
        }

        var cell = this[x, y];

        if (cell.IsVisible || cell.IsMarkedWithFlag)
        {
            return;
        }

        fieldsToOpen--;
        cell.IsVisible = true;

        if (cell.IsMine)
        {
            GameOverProcedure(false);
            return;
        }

        if (cell.NeighbourMineCount == 0)
        {
            foreach ((var nx, var ny) in GetNeighbours(x, y))
            {
                OpenField(nx, ny);
            }
        }

        if (fieldsToOpen == 0)
        {
            GameOverProcedure(true);
        }
    }

    public bool SetMine(int x, int y)
    {
        if (field[x, y].IsMine)
        {
            return false;
        }

        field[x, y].IsMine = true;
        IncrementNeighbourMineCount(x, y);
        fieldsToOpen--;

        return true;
    }

    private void InitializeField()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                field[x, y] = new FieldValue();
            }
        }
    }

    private void GameOverProcedure(bool won)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                field[x, y].IsVisible = true;
            }
        }

        GameState = won ? MinesweeperGameState.Won : MinesweeperGameState.Lost;
    }

    private void IncrementNeighbourMineCount(int x, int y)
    {
        foreach ((var nx, var ny) in GetNeighbours(x, y, true))
        {
            field[nx, ny].NeighbourMineCount++;
        }
    }

    private IEnumerable<(int X, int Y)> GetNeighbours(int x, int y, bool includeCenter = false)
    {
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                // https://github.com/SonarSource/sonar-dotnet/issues/7088
                if (!includeCenter && offsetX == 0 && offsetY == 0)
#pragma warning restore S2589
                {
                    continue;
                }

                var tx = x + offsetX;
                var ty = y + offsetY;

                if (tx < 0 || ty < 0 || tx >= Width || ty >= Height)
                {
                    continue;
                }

                yield return (tx, ty);
            }
        }
    }
}
