using OpenMacroBoard.SDK;
using System;

namespace OpenMacroBoard.Examples.XorFlip;

public class MacroBoardHost : IXorFlipHost
{
    private readonly IMacroBoard board;
    private readonly KeyBitmap keyBlocked;
    private readonly KeyBitmap keyOn;
    private readonly KeyBitmap keyOff;
    private readonly KeyBitmap gameWon;

    public MacroBoardHost(IMacroBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        this.board = board;
        board.KeyStateChanged += Board_KeyStateChanged;

        Width = board.Keys.CountX;
        Height = board.Keys.CountY;

        keyBlocked = KeyBitmap.Create.FromRgb(100, 0, 0);
        keyOff = KeyBitmap.Black;
        keyOn = KeyBitmap.Create.FromRgb(255, 255, 255);
        gameWon = KeyBitmap.Create.FromRgb(100, 100, 255);
    }

    public event EventHandler<ButtonPosEventArgs> ButtonPressed;

    public int Width { get; }
    public int Height { get; }

    public void DrawImages(XorFlipButtonState[,] buttonStates)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var keyId = KeyTransform(x, y);

                var key = buttonStates[x, y] switch
                {
                    XorFlipButtonState.Blocked => keyBlocked,
                    XorFlipButtonState.On => keyOn,
                    XorFlipButtonState.Off => keyOff,
                    _ => throw new NotImplementedException(),
                };

                board.SetKeyBitmap(keyId, key);
            }
        }
    }

    public void GameWon()
    {
        board.SetKeyBitmap(gameWon);
    }

    private void Board_KeyStateChanged(object sender, KeyEventArgs e)
    {
        if (e.IsDown)
        {
            (var x, var y) = KeyTransform(e.Key);
            ButtonPressed?.Invoke(this, new ButtonPosEventArgs(x, y));
        }
    }

    private int KeyTransform(int x, int y)
    {
        return y * Width + x;
    }

    private (int X, int Y) KeyTransform(int i)
    {
        var x = i % Width;
        var y = i / Width;

        return (x, y);
    }
}
