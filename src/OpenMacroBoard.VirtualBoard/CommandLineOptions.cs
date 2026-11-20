using CommandLine;

namespace OpenMacroBoard.VirtualBoard;

internal sealed class CommandLineOptions
{
    [Option("binding", Required = false, Default = "127.0.0.1", HelpText = "The IP address to listen on, for the board and for the page. Use 0.0.0.0 to listen on all addresses. The default only listens locally.")]
    public string? BindingIpAddress { get; set; }

    [Option("device-name", Required = false, HelpText = "The name of the board, shown on the page and reported to applications.")]
    public string? DeviceName { get; set; }

    [Option("hide-ip-titlebar", Required = false, HelpText = "Hides the IP address and port of the board on the page.")]
    public bool HideIpTitleBar { get; set; }

    [Option(HelpText = "The port applications use to reach the board (SocketIO). Default: 27621.")]
    public ushort? Port { get; set; }

    [Option("web-port", Required = false, HelpText = "The port of the page that shows the board. Default: the board port plus 1000.")]
    public ushort? WebPort { get; set; }

    [Option("open-browser", Required = false, HelpText = "Opens the page in the default browser after the start.")]
    public bool OpenBrowser { get; set; }

    [Option(HelpText = "Key count in X direction (columns).")]
    public int KeysX { get; set; } = 5;

    [Option(HelpText = "Key count in Y direction (rows).")]
    public int KeysY { get; set; } = 3;

    [Option(HelpText = "Image size of the keys in pixels.")]
    public int KeySize { get; set; } = 72;

    [Option(HelpText = "Gap between the keys in pixels.")]
    public int KeyGap { get; set; } = 25;
}
