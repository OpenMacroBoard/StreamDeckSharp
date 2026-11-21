namespace StreamDeckSharp.Internals.HidComDriver;

/// <summary>
/// This is used to convert between key ID conventions.
/// </summary>
/// <remarks>
/// <para>The original stream deck has a pretty weird way of enumerating keys.
/// Index 0 starts right top and they are enumerated right to left,
/// and top to bottom. Most developers would expect it to be left-to-right
/// instead of right-to-left, so we change that ;-)</para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.Experimental(StreamDeckSharp.Internals.InternalsDiagnostic.Id, Message = StreamDeckSharp.Internals.InternalsDiagnostic.Message)]
public interface IKeyIdMapper
{
    /// <summary>
    /// Converts from LTR convention to native device ID convention.
    /// </summary>
    /// <param name="extKeyId">Key id in LTR convention.</param>
    /// <returns>Return the key id in native device convention.</returns>
    int ExtKeyIdToHardwareKeyId(int extKeyId);

    /// <summary>
    /// Converts from native device ID convention to LTR convention.
    /// </summary>
    /// <param name="hardwareKeyId">Key id in native device convention.</param>
    /// <returns>Returns the key id in LTR convention.</returns>
    int HardwareKeyIdToExtKeyId(int hardwareKeyId);
}
