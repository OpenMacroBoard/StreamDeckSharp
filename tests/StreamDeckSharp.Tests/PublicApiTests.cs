using OpenMacroBoard.Meta.TestUtils;
using System.Threading.Tasks;
using Xunit;

namespace StreamDeckSharp.Tests;

/// <summary>
/// Snapshots of the public API, so every change of it shows up in the diff of a commit.
/// </summary>
public class PublicApiTests
{
    public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

    [Fact]
    public async Task StableApiIsAsExpected()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory().UseFileName("StreamDeckSharp");

        await Verifier.VerifyAsync(PublicApiSnapshot.GetStableApi(typeof(StreamDeck).Assembly));
    }

    [Fact]
    public async Task InternalsApiIsAsExpected()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory().UseFileName("StreamDeckSharp.Internals");

        await Verifier.VerifyAsync(PublicApiSnapshot.GetInternalsApi(typeof(StreamDeck).Assembly));
    }
}
