using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;

namespace OpenMacroBoard.Tests;

public static class ExtendedVerifierImageExtensions
{
    public static Task VerifyAsync(this ExtendedVerifySettings settings, KeyBitmap keyBitmap)
    {
        return settings
            .WithFileNameSuffix("_KeyBitmapImg")
            .VerifyImageAsync(keyBitmap);
    }
}
