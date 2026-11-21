namespace StreamDeckSharp.Internals.HidComDriver;

/// <summary>
/// The identity key mapper does not transformation at all
/// and returns the same ID back that was given.
/// </summary>
[System.Diagnostics.CodeAnalysis.Experimental(StreamDeckSharp.Internals.InternalsDiagnostic.Id, Message = StreamDeckSharp.Internals.InternalsDiagnostic.Message)]
public class IdentityKeyMapper : IKeyIdMapper
{
    /// <inheritdoc/>
    public int ExtKeyIdToHardwareKeyId(int extKeyId)
    {
        return extKeyId;
    }

    /// <inheritdoc/>
    public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
    {
        return hardwareKeyId;
    }
}
