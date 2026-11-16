using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Reflection;

namespace OpenMacroBoard.Examples.DrawFullScreen;

internal static class Program
{
    private static void Main()
    {
        using var deck = ExampleHelper.OpenBoard();
        using var bmp = LoadExampleImageFromResources();

        deck.DrawFullScreenBitmap(bmp, ResizeMode.Crop);
        ExampleHelper.WaitForKeyToExit();
    }

    private static Image LoadExampleImageFromResources()
    {
        const string resourceName = "OpenMacroBoard.Examples.DrawFullScreen.ExampleImageText.jpg";

        var asm = Assembly.GetExecutingAssembly();
        using var resStream = asm.GetManifestResourceStream(resourceName);

        return Image.Load(resStream);
    }
}
