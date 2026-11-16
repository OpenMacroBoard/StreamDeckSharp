using OpenMacroBoard.Meta.TestUtils;
using System.Threading.Tasks;

namespace OpenMacroBoard.Tests;

public static class ExtendedVerifierImageExtensions
{
    public static async Task VerifyAsync(this ExtendedVerifySettings settings, FakeMacroBoard target)
    {
        await settings
            .WithFileNameSuffix("_Meta")
            .VerifyAsync(target.GetMetaData());

        await settings
            .WithFileNameSuffix("_Image")
            .VerifyAsync(target.BoardImage);
    }
}
