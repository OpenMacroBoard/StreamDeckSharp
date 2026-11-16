using System;

namespace OpenMacroBoard.Examples.XorFlip;

public class ButtonPosEventArgs : EventArgs
{
    public ButtonPosEventArgs(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }
}
