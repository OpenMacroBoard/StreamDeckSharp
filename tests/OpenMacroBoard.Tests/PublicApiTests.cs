using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Snapshots of the public API, so every change of it shows up in the diff of a commit.
/// </summary>
public class PublicApiTests
{
    public static TheoryData<string> Libraries { get; } =
    [
        "OpenMacroBoard.SDK",
        "OpenMacroBoard.SocketIO",
    ];

    public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

    [Theory]
    [MemberData(nameof(Libraries))]
    public async Task StableApiIsAsExpected(string library)
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory().UseFileName(library);

        await Verifier.VerifyAsync(PublicApiSnapshot.GetStableApi(GetAssembly(library)));
    }

    [Theory]
    [MemberData(nameof(Libraries))]
    public async Task InternalsApiIsAsExpected(string library)
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory().UseFileName(library + ".Internals");

        await Verifier.VerifyAsync(PublicApiSnapshot.GetInternalsApi(GetAssembly(library)));
    }

    private static Assembly GetAssembly(string library)
    {
        return library == "OpenMacroBoard.SDK"
            ? typeof(KeyBitmap).Assembly
            : typeof(SocketIOBoardListener).Assembly;
    }
}
