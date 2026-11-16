using System;

namespace OpenMacroBoard.Examples.XorFlip;

public class XorFlipGame
{
    private readonly IXorFlipHost host;
    private readonly Random rnd = new();
    private readonly XorFlipButtonState[,] board;

    public XorFlipGame(IXorFlipHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        this.host = host;
        this.host.ButtonPressed += Host_ButtonPressed;

        board = new XorFlipButtonState[host.Width, host.Height];
        StartRandomGame();
    }

    private void Host_ButtonPressed(object sender, ButtonPosEventArgs e)
    {
        if (IsGameWon())
        {
            StartRandomGame();
            return;
        }

        Press(e.X, e.Y);
    }

    private void StartRandomGame()
    {
        // prevent endless loop if
        // the result is always solvable
        var maxTries = 10;

        do
        {
            TurnEverythingOn();

            // Blow two random blocks (may overlap, which is fine)
            BlockRandomPosition();
            BlockRandomPosition();

            FlipEverythingRandomly();
            maxTries--;
        }
        while (maxTries > 0 && IsGameWon());

        host.DrawImages(board);
    }

    private bool IsGameWon()
    {
        for (int y = 0; y < host.Height; y++)
        {
            for (int x = 0; x < host.Width; x++)
            {
                if (board[x, y] == XorFlipButtonState.Off)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void FlipEverythingRandomly()
    {
        for (int y = 0; y < host.Height; y++)
        {
            for (int x = 0; x < host.Width; x++)
            {
                if (rnd.NextDouble() > 0.5)
                {
                    Press(x, y);
                }
            }
        }
    }

    private void TurnEverythingOn()
    {
        for (int y = 0; y < host.Height; y++)
        {
            for (int x = 0; x < host.Width; x++)
            {
                board[x, y] = XorFlipButtonState.On;
            }
        }
    }

    private void BlockRandomPosition()
    {
        var x = rnd.Next(0, host.Width);
        var y = rnd.Next(0, host.Height);

        board[x, y] = XorFlipButtonState.Blocked;
    }

    private void Press(int x, int y)
    {
        if (board[x, y] == XorFlipButtonState.Blocked)
        {
            return;
        }

        Invert(x, y);

        var origX = x;
        var origY = y;

        // invert left side
        x = origX - 1;

        while (x >= 0 && board[x, y] != XorFlipButtonState.Blocked)
        {
            Invert(x, y);
            x--;
        }

        // invert right side
        x = origX + 1;

        while (x < host.Width && board[x, y] != XorFlipButtonState.Blocked)
        {
            Invert(x, y);
            x++;
        }

        x = origX;  // revert x

        // invert top side
        y = origY - 1;

        while (y >= 0 && board[x, y] != XorFlipButtonState.Blocked)
        {
            Invert(x, y);
            y--;
        }

        // invert bottom side
        y = origY + 1;

        while (y < host.Height && board[x, y] != XorFlipButtonState.Blocked)
        {
            Invert(x, y);
            y++;
        }

        host.DrawImages(board);

        if (IsGameWon())
        {
            host.GameWon();
        }
    }

    private void Invert(int x, int y)
    {
        if (board[x, y] == XorFlipButtonState.Off)
        {
            board[x, y] = XorFlipButtonState.On;
        }
        else if (board[x, y] == XorFlipButtonState.On)
        {
            board[x, y] = XorFlipButtonState.Off;
        }
    }
}
