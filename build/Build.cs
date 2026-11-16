using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

#pragma warning disable S1144   // Unused private types or members should be removed
#pragma warning disable S3903   // Types should be defined in named namespaces
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0022 // Use expression body for method

sealed class Build : NukeBuild
{
    public static int Main()
    {
        return Execute<Build>(x => x.Pack);
    }

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    [Solution]
    readonly Solution Solution;
#pragma warning restore CS8618

    AbsolutePath SourceDirectory
        => RootDirectory / "src";

    AbsolutePath OutputDirectory
        => RootDirectory / "output";

    AbsolutePath OpenMacroBoardSdkDirectory
        => SourceDirectory / "OpenMacroBoard.SDK";

    AbsolutePath WebSiteDirectory
        => RootDirectory / "website";

    readonly string[] NuGetPackProjects =
    [
        "StreamDeckSharp",
        "OpenMacroBoard.SDK",
        "OpenMacroBoard.SocketIO",
        "OpenMacroBoard.SDK.KeyBitmap.GDI"
    ];

    Target UpdateDocs => _ => _
        .Executes(() =>
        {
            var sdkReadme = OpenMacroBoardSdkDirectory / "README.md";
            var webSiteReadme = WebSiteDirectory / "README.md";

            CodeSampleUpdater.Run(sdkReadme);
            sdkReadme.Copy(webSiteReadme, ExistsPolicy.FileOverwrite);
        });

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory
                .GlobDirectories("**/bin", "**/obj")
                .ForEach(x => x.DeleteDirectory());

            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Pack => _ => _
        .Requires(() => Configuration == Configuration.Release)
        .After(Clean)
        .Executes(() =>
        {
            OutputDirectory.CreateOrCleanDirectory();

            foreach (var projectName in NuGetPackProjects)
            {
                var project = Solution.GetExactProject(projectName);

                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                );

                var binDir = project.Directory / "bin" / Configuration;
                var nupkgFile = binDir.GlobFiles("*.nupkg").Single();

                nupkgFile.CopyToDirectory(OutputDirectory, ExistsPolicy.MergeAndOverwrite);
            }

            var virtualBoardProject = Solution.GetExactProject("OpenMacroBoard.VirtualBoard");
            var releasePath = virtualBoardProject.Directory / "bin" / "Release" / "net10.0-windows" / "win-x64" / "publish";

            releasePath.DeleteDirectory();

            DotNetPublish(s => s
                .SetProject(virtualBoardProject)
                .SetConfiguration(Configuration)
                .EnablePublishSingleFile()
                .DisableSelfContained()
            );

            releasePath
                .GlobFiles("*.xml", "*.pdb", "*.deps.json")
                .ForEach(f => f.DeleteFile());

            var virtualBoardVersion = GetVersion(virtualBoardProject.Path);

            Assert.True(
                releasePath.GetFiles().Count() == 1,
                "Publish output should only contain a single file at this point"
            );

            var binaryPath = releasePath / "OpenMacroBoard.VirtualBoard.exe";
            binaryPath.Copy(OutputDirectory / $"VirtualBoard_{virtualBoardVersion}_x64.exe");
        });

    private string GetVersion(string project)
    {
        return XDocument
            .Load(project)
            .Descendants()
            .Where(d => d.Name.LocalName == "PropertyGroup")
            .SelectMany(d => d
                .Descendants()
                .Where(x => x.Name.LocalName == "Version")
            )
            .FirstOrDefault()
            ?.Value
            ?? throw new KeyNotFoundException()
            ;
    }

    private void RunCodeInRoot(string toolPath, string arguments)
    {
        var proc = ProcessTasks.StartProcess(toolPath,
                workingDirectory: RootDirectory,
                arguments: arguments
            );

        proc.WaitForExit();
        proc.AssertZeroExitCode();
    }
}
