namespace StreamDeckSharp.Internals.HidComDriver;

/// <summary>
/// Classic StreamDeck ID mapper to fix RTL native device IDs.
/// </summary>
[System.Diagnostics.CodeAnalysis.Experimental(StreamDeckSharp.Internals.InternalsDiagnostic.Id, Message = StreamDeckSharp.Internals.InternalsDiagnostic.Message)]
public class StreamDeck5HorizontalFlip : IKeyIdMapper
{
    /// <inheritdoc/>
    public int ExtKeyIdToHardwareKeyId(int extKeyId)
    {
        return FlipIdsHorizontal(extKeyId);
    }

    /// <inheritdoc/>
    public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
    {
        return FlipIdsHorizontal(hardwareKeyId);
    }

    private static int FlipIdsHorizontal(int keyId)
    {
        var diff = ((keyId % 5) - 2) * -2;
        return keyId + diff;
    }
}
