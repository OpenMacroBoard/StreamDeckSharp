using System.Net;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// Everything that defines one virtual board.
/// </summary>
internal sealed record VirtualBoardSettings
{
    public const ushort DefaultBoardPort = 27621;
    public const int WebPortOffset = 1000;

    public required string DeviceName { get; init; }

    /// <summary>
    /// The address the board (SocketIO) and the page listen on.
    /// </summary>
    public IPAddress BindAddress { get; init; } = IPAddress.Loopback;

    /// <summary>
    /// The port applications use to reach the board. Zero lets the operating system choose.
    /// </summary>
    public ushort BoardPort { get; init; } = DefaultBoardPort;

    /// <summary>
    /// The port of the page. Zero lets the operating system choose.
    /// </summary>
    public ushort WebPort { get; init; } = DefaultBoardPort + WebPortOffset;

    public int KeysX { get; init; } = 5;

    public int KeysY { get; init; } = 3;

    public int KeySize { get; init; } = 72;

    public int KeyGap { get; init; } = 25;

    /// <summary>
    /// Whether the page shows the address and port of the board.
    /// </summary>
    public bool ShowBoardEndpoint { get; init; } = true;
}
