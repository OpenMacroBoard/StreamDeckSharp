using SixLabors.ImageSharp;
using System.IO;
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

    public static byte[] LoadBytes(string assetName)
    {
        using var resStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"OpenMacroBoard.Tests.Assets.{assetName}");

        using var buffer = new MemoryStream();
        resStream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
