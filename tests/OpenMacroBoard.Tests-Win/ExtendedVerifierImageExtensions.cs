using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;

namespace OpenMacroBoard.Tests;

public static class ExtendedVerifierImageExtensions
{
    public static Task VerifyAsync(this ExtendedVerifySettings settings, KeyBitmap keyBitmap)
    {
        var dataAccess = (IKeyBitmapDataAccess)keyBitmap;

        using var img = dataAccess.ToImage();

        return settings
            .WithFileNameSuffix("_KeyBitmapImg")
            .VerifyAsync(img);
    }
}
