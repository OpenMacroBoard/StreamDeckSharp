using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpenMacroBoard.Meta.TestUtils;

public static class ExtendedVerifySettingsExtensions
{
    public static void Initialize(
        this ExtendedVerifySettings settings,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = -1
    )
    {
        settings.CallerLineNumber = lineNumber;
        settings.CallerMemberName = memberName;
        settings.CallerFilePath = filePath;

        var testClassFileName = Path.GetFileNameWithoutExtension(filePath);
        settings.UseFileName($"{testClassFileName}.{memberName}");

        // Use a file name for the test results. Overrides the
        // `{TestClassName}.{TestMethodName}_{Parameters}` of
        // `{Directory}/{TestClassName}.{TestMethodName}_{ParameterUniqueEtc}.verified.{extension}`.
    }

    public static ExtendedVerifySettings UseFileNameAsDirectory(this ExtendedVerifySettings settings)
    {
        return settings.UseAppendedFolder(settings.FileName).UseFileName(string.Empty);
    }

    public static ExtendedVerifySettings WithFileNameAsDirectory(this ExtendedVerifySettings settings)
    {
        return settings.Clone().UseFileNameAsDirectory();
    }

    public static ExtendedVerifySettings UseFileName(this ExtendedVerifySettings settings, string fileName)
    {
        settings.FileName = fileName;
        return settings;
    }

    public static ExtendedVerifySettings WithFileName(this ExtendedVerifySettings settings, string fileName)
    {
        return settings.Clone().UseFileName(fileName);
    }

    public static ExtendedVerifySettings UseFileNameSuffix(this ExtendedVerifySettings settings, string suffix)
    {
        settings.FileName += suffix;
        return settings;
    }

    public static ExtendedVerifySettings WithFileNameSuffix(this ExtendedVerifySettings settings, string suffix)
    {
        return settings.Clone().UseFileNameSuffix(suffix);
    }

    public static ExtendedVerifySettings UseUniqueSuffix(this ExtendedVerifySettings settings, string suffix)
    {
        settings.FileName += $"_{suffix}";
        return settings;
    }

    public static ExtendedVerifySettings WithUniqueSuffix(this ExtendedVerifySettings settings, string suffix)
    {
        return settings.Clone().UseUniqueSuffix(suffix);
    }

    public static ExtendedVerifySettings UseAppendedFolder(this ExtendedVerifySettings settings, string folderName)
    {
        settings.Directory = Path.Combine(settings.Directory, folderName);
        return settings;
    }

    public static ExtendedVerifySettings WithAppendedFolder(this ExtendedVerifySettings settings, string folderName)
    {
        return settings.Clone().UseAppendedFolder(folderName);
    }

    public static ExtendedVerifySettings UseUniqueClrSuffix(this ExtendedVerifySettings settings)
    {
        return settings.UseUniqueSuffix($"CLR=v{Environment.Version}");
    }

    public static ExtendedVerifySettings WithUniqueClrSuffix(this ExtendedVerifySettings settings)
    {
        return settings.Clone().UseUniqueClrSuffix();
    }
}
