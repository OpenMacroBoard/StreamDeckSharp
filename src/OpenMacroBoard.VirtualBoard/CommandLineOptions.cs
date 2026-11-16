using CommandLine;

namespace OpenMacroBoard.VirtualBoard;

internal sealed class CommandLineOptions
{
    [Option("binding", Required = false, Default = "127.0.0.1", HelpText = "Defines a binding IP Address. Use 0.0.0.0 to listen on all connections. Default will only listen locally.")]
    public string? BindingIpAddress { get; set; }

    [Option("device-name", Required = false)]
    public string? DeviceName { get; set; }

    [Option("hide-ip-titlebar", Required = false)]
    public bool HideIpTitleBar { get; set; }

    [Option]
    public ushort? Port { get; set; }

    [Option]
    public int KeysX { get; set; } = 5;

    [Option]
    public int KeysY { get; set; } = 3;

    [Option]
    public int KeySize { get; set; } = 72;

    [Option]
    public int KeyGap { get; set; } = 25;
}
