namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// A handle that can be used to open an <see cref="IMacroBoard"/> instance.
    /// </summary>
    public interface IDeviceReferenceHandle
    {
        /// <summary>
        /// Opens a live <see cref="IMacroBoard"/> instance
        /// </summary>
        /// <returns></returns>
        IMacroBoard Open();
    }
}
