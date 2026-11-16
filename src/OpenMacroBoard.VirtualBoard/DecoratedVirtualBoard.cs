using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using System;
using System.Reflection;

namespace OpenMacroBoard.VirtualBoard;

internal sealed class DecoratedVirtualBoard : MacroBoardAdapter
{
    private readonly VirtualBoardViewModel nakedBoard;

    private bool isLogoShown = false;

    public DecoratedVirtualBoard(VirtualBoardViewModel virtualBoard)
        : base(virtualBoard)
    {
        ArgumentNullException.ThrowIfNull(virtualBoard);

        nakedBoard = virtualBoard;
        ShowLogo();
    }

    public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        if (isLogoShown)
        {
            nakedBoard.ClearKeys();
            isLogoShown = false;
        }

        nakedBoard.SetKeyBitmap(keyId, bitmapData);
    }

    public override void ShowLogo()
    {
        if (isLogoShown)
        {
            return;
        }

        using var logoStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("OpenMacroBoard.VirtualBoard.OpenMacroBoard-Logo.png");

        ArgumentNullException.ThrowIfNull(logoStream);

        using var logo = Image.Load(logoStream);
        nakedBoard.DrawFullScreenBitmap(logo);

        isLogoShown = true;
    }
}
