using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using System.Reflection;

namespace OpenMacroBoard.Examples.DrawFullScreen;

internal static class Program
{
    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();
        var bmp = LoadExampleImageFromResources();

        deck.DrawFullScreenBitmap(bmp, OmbResizeMode.Crop);
        ExampleHelper.WaitForKeyToExit();
    }

    private static KeyBitmap LoadExampleImageFromResources()
    {
        const string resourceName = "OpenMacroBoard.Examples.DrawFullScreen.ExampleImageText.jpg";

        var asm = Assembly.GetExecutingAssembly();
        using var resStream = asm.GetManifestResourceStream(resourceName);

        return KeyBitmap.Create.FromStream(resStream);
    }
}
