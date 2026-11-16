using Newtonsoft.Json;

namespace OpenMacroBoard.SocketIO;

/// <summary>
/// Details about the key layout for a KissIpBoard.
/// </summary>
public class KeyDetails
{
    // Note: This class crosses process boundaries (via TCP).
    // Keep that in mind before you changes things.

    // Note: Property names are intentionally short
    // to reduce unnecessary overhead.

    /// <summary>
    /// The number of keys in the horizontal (x) direction.
    /// </summary>
    [JsonProperty("x")]
    public int CountX { get; set; }

    /// <summary>
    /// The number of keys in the vertical (y) direction.
    /// </summary>
    [JsonProperty("y")]
    public int CountY { get; set; }

    /// <summary>
    /// The size of the key images in pixel.
    /// </summary>
    [JsonProperty("s")]
    public int KeySize { get; set; }

    /// <summary>
    /// The size of the gap between the keys in pixel.
    /// </summary>
    [JsonProperty("g")]
    public int GapSize { get; set; }
}
