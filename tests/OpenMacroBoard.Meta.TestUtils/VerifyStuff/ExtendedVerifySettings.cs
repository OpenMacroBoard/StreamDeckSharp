using System;
using VerifyTests;

namespace OpenMacroBoard.Meta.TestUtils;

public class ExtendedVerifySettings : ICloneable
{
    public string CallerFilePath { get; set; }
    public string CallerMemberName { get; set; }
    public int CallerLineNumber { get; set; }

    public string Directory { get; set; }
    public string FileName { get; set; }

    public bool AutoVerify { get; set; }

    public VerifySettings BuildVerifySettings()
    {
        var settings = new VerifySettings();

        if (string.IsNullOrEmpty(FileName))
        {
            settings.UseFileName("Snapshot");
        }
        else
        {
            settings.UseFileName(FileName);
        }

        settings.UseDirectory(Directory);

        if (AutoVerify)
        {
            settings.AutoVerify(false);
        }

        return settings;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }

    public ExtendedVerifySettings Clone()
    {
        return (ExtendedVerifySettings)MemberwiseClone();
    }
}
