namespace OpenMacroBoard.Meta.TestUtils;

public static class DefaultVerifySettings
{
    static DefaultVerifySettings()
    {
        PngSnapshot.Initialize();
    }

    public static ExtendedVerifySettings Build(bool autoVerify = false)
    {
        return new ExtendedVerifySettings
        {
            Directory = "VerifiedSnapshots",
            AutoVerify = autoVerify,
        };
    }
}
