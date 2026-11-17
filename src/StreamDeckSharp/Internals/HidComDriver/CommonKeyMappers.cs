namespace StreamDeckSharp.Internals.HidComDriver;

/// <summary>
/// Collection of typical key mappers.
/// </summary>
[System.Diagnostics.CodeAnalysis.Experimental(StreamDeckSharp.Internals.InternalsDiagnostic.Id, Message = StreamDeckSharp.Internals.InternalsDiagnostic.Message)]
public static class CommonKeyMappers
{
    /// <summary>
    /// No mapping. Keep IDs the same.
    /// </summary>
    public static IdentityKeyMapper Identity { get; } = new();

    /// <summary>
    /// Classic StreamDeck ID mapper to fix RTL native device IDs.
    /// </summary>
    public static StreamDeck5HorizontalFlip StreamDeck5HorizontalFlip { get; } = new();
}
