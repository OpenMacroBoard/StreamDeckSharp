using Nuke.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable S3903 // Types should be defined in named namespaces

/// <summary>
/// A quick and dirty implementation
/// </summary>
/// <remarks>
/// <para>
/// This is a quick and dirty tool to update examples in the documentation.
/// I looked at a bunch of tools, but most of them only work with specific format, are not available
/// via nuget or are just to complicated for such a simple task.
/// </para>
/// <para>
/// The sample updater works by giving it a markdown file (which contains region markers) and copies the referenced
/// example code into the markdown file. The referenced source also contains region markers.
/// </para>
/// <para>
/// Additionally we remove unnecessary indentation to make it more readable.
/// </para>
/// <para>
/// The file references inside the markdown file are relative to the markdown file itself.
/// </para>
/// </remarks>
internal sealed partial class CodeSampleUpdater
{
    readonly AbsolutePath markdownFile;

    public CodeSampleUpdater(AbsolutePath markdownFile)
    {
        ArgumentNullException.ThrowIfNull(markdownFile);
        this.markdownFile = markdownFile;
    }

    public static void Run(AbsolutePath markdownFile)
    {
        var updater = new CodeSampleUpdater(markdownFile);
        updater.Run();
    }

    public void Run()
    {
        var targetDocumentation = new List<string>();
        var documentationLines = File.ReadAllLines(markdownFile);

        for (int i = 0; i < documentationLines.Length; i++)
        {
            var lineMarker = GetLineMarker(documentationLines[i]);

            if (lineMarker is null)
            {
                targetDocumentation.Add(documentationLines[i]);
                continue;
            }

            if (lineMarker is CodeEndMarker)
            {
                throw new NotSupportedException("Unexpected end code marker.");
            }

            if (lineMarker is not CodeStartMarker startMarker)
            {
                throw new NotSupportedException("Unknown marker.");
            }

            var targetFile = markdownFile.Parent / startMarker.RelativePath;
            var ext = File.ReadAllLines(targetFile);

            bool IsStartMarker(string l)
            {
                var m = GetLineMarker(l);

                if (m is null)
                {
                    return false;
                }

                if (m is CodeEndMarker)
                {
                    return false;
                }

                if (m is not CodeStartMarker stC)
                {
                    return false;
                }

                return stC.SnippetName == startMarker.SnippetName;
            }

            var codeBlockSourceIndentation = ext
                .SkipWhile(l => !IsStartMarker(l))
                .Skip(1)
                .TakeWhile(l => !IsEndMarker(l))
                .ToList();

            var codeBlockTargetIndentation = ReduceCodeBlockIndentation(codeBlockSourceIndentation);

            targetDocumentation.Add(documentationLines[i]);
            targetDocumentation.Add($"```{startMarker.Language}");
            targetDocumentation.AddRange(codeBlockTargetIndentation);
            targetDocumentation.Add("```");

            do
            {
                i++;
            }
            while (GetLineMarker(documentationLines[i]) is not CodeEndMarker);

            targetDocumentation.Add(documentationLines[i]);
        }

        File.WriteAllLines(markdownFile, targetDocumentation);
    }

    private static bool IsEndMarker(string line)
    {
        var m = GetLineMarker(line);
        return m is CodeEndMarker;
    }


    private List<string> ReduceCodeBlockIndentation(IReadOnlyList<string> codeLines)
    {
        var prefix
            = GetCommonPrefix(
                codeLines
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(GetLeadingWhitespace)
                    .ToList()
            );

        return codeLines
            .Select(l => l[Math.Min(l.Length, prefix.Length)..])
            .ToList();
    }

    private string GetLeadingWhitespace(string line)
    {
        return new string(line.TakeWhile(char.IsWhiteSpace).ToArray());
    }

    private string GetCommonPrefix(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count < 1)
        {
            return string.Empty;
        }

        if (lines.Count == 1)
        {
            return lines[0];
        }

        var commonPrefix = new StringBuilder();
        var charPos = 0;

        while (true)
        {
            if (charPos >= lines[0].Length)
            {
                return commonPrefix.ToString();
            }

            var candidate = lines[0][charPos];

            for (int i = 1; i < lines.Count; i++)
            {
                if (charPos >= lines[i].Length || lines[i][charPos] != candidate)
                {
                    return commonPrefix.ToString();
                }
            }

            commonPrefix.Append(candidate);
            charPos++;
        }
    }

    private static IMarker? GetLineMarker(string line)
    {
        if (!line.Contains("<!--coderef:"))
        {
            return null;
        }

        if (line.Contains("<!--coderef:end-->"))
        {
            return CodeEndMarker.Instance;
        }

        var res = CodeRefBeginRegex().Match(line);

        if (res is null)
        {
            // invalid code ref?
            return null;
        }

        if (res.Groups.Count != 2)
        {
            // invalid code ref?
            return null;
        }

        var details = res.Groups[1].Value;

        if (!details.EndsWith("--", StringComparison.Ordinal))
        {
            // invalid ref.
            return null;
        }

        var d = details[..^2];
        var parts = d.Split('|');

        var marker = new CodeStartMarker(parts[0]);

        if (parts.Length > 1)
        {
            marker.RelativePath = parts[1];
        }

        if (parts.Length > 2)
        {
            marker.Language = parts[2];
        }

        return marker;
    }

    private interface IMarker
    {
    }

    private sealed class CodeStartMarker : IMarker
    {
        public CodeStartMarker(string snippetName)
        {
            SnippetName = snippetName;
        }

        public string SnippetName { get; }
        public string? Language { get; set; }
        public string? RelativePath { get; set; }
    }

    private sealed class CodeEndMarker : IMarker
    {
        public static CodeEndMarker Instance { get; } = new CodeEndMarker();
    }

    [GeneratedRegex("<!--coderef:([^>]*)>")]
    private static partial Regex CodeRefBeginRegex();
}
