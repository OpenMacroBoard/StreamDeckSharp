using OpenMacroBoard.SDK;
using System;
using System.Reflection;
using System.Threading;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// Adds the behaviour of a real device to the board in memory: it shows a logo until the
/// first key image arrives.
/// </summary>
internal sealed class DecoratedVirtualBoard : MacroBoardAdapter
{
    private readonly Lock gate = new();
    private readonly InMemoryMacroBoard nakedBoard;

    private bool isLogoShown;

    public DecoratedVirtualBoard(InMemoryMacroBoard virtualBoard)
        : base(virtualBoard)
    {
        ArgumentNullException.ThrowIfNull(virtualBoard);

        nakedBoard = virtualBoard;
        ShowLogo();
    }

    public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        lock (gate)
        {
            if (isLogoShown)
            {
                nakedBoard.ClearKeys();
                isLogoShown = false;
            }

            nakedBoard.SetKeyBitmap(keyId, bitmapData);
        }
    }

    public override void ShowLogo()
    {
        lock (gate)
        {
            if (isLogoShown)
            {
                return;
            }

            using var logoStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("OpenMacroBoard.VirtualBoard.OpenMacroBoard-Logo.png");

            ArgumentNullException.ThrowIfNull(logoStream);

            var logo = KeyBitmap.Create.FromStream(logoStream);
            nakedBoard.DrawFullScreenBitmap(logo);

            isLogoShown = true;
        }
    }
}
