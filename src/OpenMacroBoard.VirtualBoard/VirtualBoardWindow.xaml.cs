using CommandLine;
using CommandLine.Text;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using System;
using System.Net;
using System.Windows;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1001  // Types that own disposable fields should be disposable

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// Interaction logic for VirtualBoardWindow.xaml
/// </summary>
public partial class VirtualBoardWindow : Window
{
    private const int DefaultPortNumber = 27621;

    private readonly SocketIOMacroBoardHost server;

    /// <summary>
    /// Creates an instance of <see cref="VirtualBoardWindow"/>.
    /// </summary>
    /// <param name="viewModel"></param>
    public VirtualBoardWindow()
    {
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();

        var res = Parser.Default.ParseArguments<CommandLineOptions>(args);
        var settings = new CommandLineOptions();
        var parsedSettings = false;

        res
            .WithParsed(x =>
            {
                if (x.KeysX <= 0 || x.KeysY <= 0)
                {
                    return;
                }

                parsedSettings = true;
                settings = x;
            });

#pragma warning disable S2589 // Boolean expressions should not be gratuitous
        if (!parsedSettings)
#pragma warning restore S2589
        {
            var text = HelpText.AutoBuild(res, int.MaxValue);
            var body = text.ToString().Replace("\r\n\r\n", "\r\n");
            MessageBox.Show(body, text.Heading);
        }

        var deviceName = settings.DeviceName ?? $"Virtual Board - {settings.KeysX}x{settings.KeysY}";
        var port = settings.Port ?? DefaultPortNumber;

        var keyPositions = new GridKeyLayout(
            settings.KeysX,
            settings.KeysY,
            settings.KeySize,
            settings.KeyGap
        );

        SetupWindowSize(keyPositions);

        var bindAddress = IPAddress.Loopback;

        if (IPAddress.TryParse(settings.BindingIpAddress, out var parsedBindAddress))
        {
            bindAddress = parsedBindAddress;
        }

        var macroBoardViewModel = new VirtualBoardViewModel(keyPositions);
        DataContext = macroBoardViewModel;

        var titleBarText = deviceName;

        if (!settings.HideIpTitleBar)
        {
            titleBarText = $"{titleBarText} ({bindAddress}:{port})";
        }

        Title = titleBarText;
        server = new(new DecoratedVirtualBoard(macroBoardViewModel), deviceName, bindAddress, port);

        Closed += VirtualBoardWindow_Closed;
    }

    private void SetupWindowSize(GridKeyLayout keyPositions)
    {
        var ratio = (double)(keyPositions.Area.Width + 2 * keyPositions.GapSize) / (keyPositions.Area.Height + 2 * keyPositions.GapSize);

        if (keyPositions.Area.Width > keyPositions.Area.Height)
        {
            ViewBox.Width = 800;
            ViewBox.Height = 800 / ratio;
        }
        else
        {
            ViewBox.Height = 800;
            ViewBox.Width = 800 / ratio;
        }

        void SizeChangeHandler(object? sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Height == 0 && e.PreviousSize.Width == 0)
            {
                return;
            }

            // Once the user resizes the window, let the content scale freely
            SizeToContent = SizeToContent.Manual;
            ViewBox.Width = double.NaN;
            ViewBox.Height = double.NaN;

            SizeChanged -= SizeChangeHandler;
        }

        SizeChanged += SizeChangeHandler;
    }

    private void VirtualBoardWindow_Closed(object? sender, EventArgs e)
    {
        server.Dispose();
    }
}
