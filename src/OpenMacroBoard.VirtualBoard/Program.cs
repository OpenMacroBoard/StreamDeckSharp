using CommandLine;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace OpenMacroBoard.VirtualBoard;

internal static class Program
{
    internal static bool TryCreateSettings(CommandLineOptions options, out VirtualBoardSettings settings, out string error)
    {
        settings = new VirtualBoardSettings { DeviceName = string.Empty };

        if (options.KeysX <= 0 || options.KeysY <= 0 || options.KeysX * options.KeysY > ushort.MaxValue)
        {
            error = "The key counts (--keysx, --keysy) must be at least 1.";
            return false;
        }

        if (options.KeySize <= 0 || options.KeySize > 1024 || options.KeyGap < 0 || options.KeyGap > 1024)
        {
            error = "The key size (--keysize) must be 1 to 1024 and the gap (--keygap) 0 to 1024.";
            return false;
        }

        if (!IPAddress.TryParse(options.BindingIpAddress ?? "127.0.0.1", out var bindAddress))
        {
            error = $"'{options.BindingIpAddress}' (--binding) is not an IP address.";
            return false;
        }

        var boardPort = options.Port ?? VirtualBoardSettings.DefaultBoardPort;
        var webPort = options.WebPort ?? boardPort + VirtualBoardSettings.WebPortOffset;

        if (webPort > ushort.MaxValue)
        {
            error = $"The default port of the page ({webPort}) is not valid. Set it with --web-port.";
            return false;
        }

        settings = new VirtualBoardSettings
        {
            DeviceName = options.DeviceName ?? $"Virtual Board - {options.KeysX}x{options.KeysY}",
            BindAddress = bindAddress,
            BoardPort = boardPort,
            WebPort = (ushort)webPort,
            KeysX = options.KeysX,
            KeysY = options.KeysY,
            KeySize = options.KeySize,
            KeyGap = options.KeyGap,
            ShowBoardEndpoint = !options.HideIpTitleBar,
        };

        error = string.Empty;
        return true;
    }

    private static async Task<int> Main(string[] args)
    {
        // The parser prints the help or the errors itself.
        var parsed = Parser.Default.ParseArguments<CommandLineOptions>(args);

        if (parsed.Tag != ParserResultType.Parsed)
        {
            return 1;
        }

        if (!TryCreateSettings(parsed.Value, out var settings, out var error))
        {
            await Console.Error.WriteLineAsync(error);
            return 1;
        }

        await using var app = await VirtualBoardApp.StartAsync(settings);

        Console.WriteLine(settings.DeviceName);
        Console.WriteLine($"  Board (for applications, SocketIO): {app.BoardEndpoint}");
        Console.WriteLine($"  Page (open it in a browser):        {app.PageUrl}");
        Console.WriteLine("Press Ctrl+C to stop.");

        if (parsed.Value.OpenBrowser)
        {
            OpenInBrowser(app.PageUrl);
        }

        await app.WaitForShutdownAsync();
        return 0;
    }

    private static void OpenInBrowser(Uri pageUrl)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(pageUrl.ToString()) { UseShellExecute = true });
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            Console.WriteLine("The browser could not be opened. Open the page yourself.");
        }
    }
}
