using SixLabors.ImageSharp;
using System.Reflection;

namespace OpenMacroBoard.Tests;

internal static class AssetLoader
{
    public static Image LoadImage(string assetName)
    {
        using var resStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"OpenMacroBoard.Tests.Assets.{assetName}");

        return Image.Load(resStream);
    }
}
