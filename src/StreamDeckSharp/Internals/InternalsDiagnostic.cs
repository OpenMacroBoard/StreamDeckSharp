namespace StreamDeckSharp.Internals;

/// <summary>
/// The diagnostic that marks the public types of the <c>Internals</c> namespaces.
/// </summary>
/// <remarks>
/// <para>
/// These types are public so nobody needs reflection to reach them, but they are not covered
/// by semantic versioning: they can change or disappear in any release. Using them reports
/// this diagnostic as an error until it is suppressed, for example with
/// <c>&lt;NoWarn&gt;$(NoWarn);OMB0001&lt;/NoWarn&gt;</c> in the project file.
/// </para>
/// </remarks>
internal static class InternalsDiagnostic
{
    /// <summary>
    /// The ID of the diagnostic.
    /// </summary>
    public const string Id = "OMB0001";

    /// <summary>
    /// The text that is shown with the diagnostic.
    /// </summary>
    public const string Message = "Internal API of StreamDeckSharp: not covered by semantic versioning.";
}
