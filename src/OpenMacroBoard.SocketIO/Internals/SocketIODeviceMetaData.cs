using Newtonsoft.Json;

namespace OpenMacroBoard.SocketIO.Internals;

/// <summary>
/// KissIpBoard meta data, which is broad-casted via UDP.
/// </summary>
internal class SocketIODeviceMetaData
{
    // Note: This class crosses process boundaries (via TCP).
    // Keep that in mind before you changes things.

    // Note: Property names are intentionally short
    // to reduce unnecessary overhead.

    /// <summary>
    /// The version of this meta structure.
    /// </summary>
    /// <remarks>
    /// <para>The value is currently not checked, but that might change in the future.</para>
    /// </remarks>
    [JsonProperty("v")]
    public int MetaVersion { get; set; }

    /// <summary>
    /// The name of the device.
    /// </summary>
    [JsonProperty("name")]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// The (fake) firmware version of the device.
    /// </summary>
    [JsonProperty("fwv")]
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>
    /// The (fake) serial number of the device.
    /// </summary>
    [JsonProperty("sn")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Details about the keyboard layout of the device.
    /// </summary>
    [JsonProperty("keys")]
    public KeyDetails Keys { get; set; } = new();
}
