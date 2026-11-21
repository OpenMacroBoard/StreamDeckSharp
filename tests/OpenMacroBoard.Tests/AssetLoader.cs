using OpenMacroBoard.SDK;
using System.IO;
using System.Reflection;

namespace OpenMacroBoard.Tests;

internal static class AssetLoader
{
    /// <summary>
    /// Loads an image asset with the built-in decoder.
    /// </summary>
    public static KeyBitmap LoadBitmap(string assetName)
    {
        return OmbImageDecoder.Default.Decode(LoadBytes(assetName));
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
