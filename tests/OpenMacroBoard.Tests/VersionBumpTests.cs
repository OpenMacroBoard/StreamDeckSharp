using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Makes sure that an acknowledged breaking change comes with a new major version.
/// </summary>
/// <remarks>
/// <para>
/// Package validation fails on breaking changes against the last released version until they
/// are listed in the CompatibilitySuppressions.xml of the project. This test fails if such a
/// file lists a break of the stable API while the major version of the project is not higher
/// than the one of the last release. Breaks of types in <c>Internals</c> namespaces do not
/// count, they are not covered by semantic versioning.
/// </para>
/// </remarks>
public class VersionBumpTests
{
    public static TheoryData<string> PackageProjects { get; } =
    [
        "src/OpenMacroBoard.SDK/OpenMacroBoard.SDK.csproj",
        "src/OpenMacroBoard.SocketIO/OpenMacroBoard.SocketIO.csproj",
        "src/OpenMacroBoard.SDK.KeyBitmap.GDI/OpenMacroBoard.SDK.KeyBitmap.GDI.csproj",
        "src/OpenMacroBoard.SDK.KeyBitmap.SkiaSharp/OpenMacroBoard.SDK.KeyBitmap.SkiaSharp.csproj",
        "src/StreamDeckSharp/StreamDeckSharp.csproj",
    ];

    [Theory]
    [MemberData(nameof(PackageProjects))]
    public void BreakingChangesOfTheStableApiComeWithANewMajorVersion(string project)
    {
        var root = GetRepositoryRoot();
        var projectFile = Path.Combine(root, project);

        var released = ReadVersion(Path.Combine(root, "package-defaults.targets"), "PackageValidationBaselineVersion");
        var current = ReadVersion(projectFile, "VersionPrefix");

        var suppressionFile = Path.Combine(Path.GetDirectoryName(projectFile)!, "CompatibilitySuppressions.xml");
        var stableBreaks = ReadSuppressedTargets(suppressionFile).Where(IsStableApi).ToList();

        if (stableBreaks.Count == 0)
        {
            return;
        }

        current.Major.Should().BeGreaterThan(
            released.Major,
            "the stable API has breaking changes compared to version {0}, for example {1}",
            released,
            stableBreaks[0]
        );
    }

    [Theory]
    [InlineData("T:OpenMacroBoard.SDK.KeyBitmap", true)]
    [InlineData("M:OpenMacroBoard.SDK.KeyBitmap.op_Equality(OpenMacroBoard.SDK.KeyBitmap,OpenMacroBoard.SDK.KeyBitmap)", true)]
    [InlineData("T:StreamDeckSharp.Internals.IStreamDeckHidComDriver", false)]
    [InlineData("T:StreamDeckSharp.Internals.HidComDriver.IKeyIdMapper", false)]
    [InlineData("M:StreamDeckSharp.Internals.HidComDriver.IKeyIdMapper.ExtKeyIdToHardwareKeyId(System.Int32)", false)]
    [InlineData("M:StreamDeckSharp.Hardware.RegisterNewHardware(StreamDeckSharp.Internals.IStreamDeckHidComDriver)", true)]
    [InlineData("", true)]
    public void TargetsAreAssignedToTheStableApiOrTheInternals(string target, bool expectedStable)
    {
        IsStableApi(target).Should().Be(expectedStable);
    }

    /// <summary>
    /// Decides by the place where a type or member is declared. A member of the stable API
    /// that only mentions an internal type in its signature still belongs to the stable API.
    /// </summary>
    private static bool IsStableApi(string target)
    {
        var parameterStart = target.IndexOf('(', StringComparison.Ordinal);
        var declaration = parameterStart < 0 ? target : target[..parameterStart];

        return !declaration.Split('.', ':').Contains("Internals", StringComparer.Ordinal);
    }

    private static List<string> ReadSuppressedTargets(string suppressionFile)
    {
        if (!File.Exists(suppressionFile))
        {
            return [];
        }

        // A suppression without a target (for example a dropped target framework) counts as
        // a break of the stable API, it is reported with an empty target.
        return XDocument
            .Load(suppressionFile)
            .Descendants("Suppression")
            .Select(suppression => suppression.Element("Target")?.Value ?? string.Empty)
            .ToList();
    }

    private static Version ReadVersion(string msbuildFile, string property)
    {
        var value = XDocument
            .Load(msbuildFile)
            .Descendants(property)
            .Select(element => element.Value)
            .Single();

        return Version.Parse(value);
    }

    private static string GetRepositoryRoot([CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile);

        while (directory is not null && !File.Exists(Path.Combine(directory, "StreamDeckSharp.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
