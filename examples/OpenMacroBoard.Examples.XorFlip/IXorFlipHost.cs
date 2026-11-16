using System;

namespace OpenMacroBoard.Examples.XorFlip;

public interface IXorFlipHost
{
    event EventHandler<ButtonPosEventArgs> ButtonPressed;

    int Width { get; }
    int Height { get; }

    void GameWon();
    void DrawImages(XorFlipButtonState[,] buttonStates);
}
