using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// A running virtual board: the board in memory, the SocketIO host that applications
/// connect to, and the web server with the page that shows the board.
/// </summary>
internal sealed class VirtualBoardApp : IAsyncDisposable
{
    private readonly WebApplication web;
    private readonly SocketIOMacroBoardHost boardHost;

    private VirtualBoardApp(WebApplication web, SocketIOMacroBoardHost boardHost, InMemoryMacroBoard board, Uri pageUrl)
    {
        this.web = web;
        this.boardHost = boardHost;

        Board = board;
        PageUrl = pageUrl;
    }

    /// <summary>
    /// The board in memory, without the logo image.
    /// </summary>
    public InMemoryMacroBoard Board { get; }

    /// <summary>
    /// The address applications use to reach the board through SocketIO.
    /// </summary>
    public IPEndPoint BoardEndpoint => boardHost.HostEndpoint;

    /// <summary>
    /// The address of the page that shows the board.
    /// </summary>
    public Uri PageUrl { get; }

    public static async Task<VirtualBoardApp> StartAsync(VirtualBoardSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var keyLayout = new GridKeyLayout(settings.KeysX, settings.KeysY, settings.KeySize, settings.KeyGap);
        var board = new InMemoryMacroBoard(keyLayout);

        var boardHost = new SocketIOMacroBoardHost(
            new DecoratedVirtualBoard(board),
            settings.DeviceName,
            settings.BindAddress,
            settings.BoardPort
        );

        try
        {
            var boardEndpoint = settings.ShowBoardEndpoint ? boardHost.HostEndpoint.ToString() : null;
            var web = BuildWebApplication(settings, board, boardEndpoint);

            await web.StartAsync();

            return new VirtualBoardApp(web, boardHost, board, GetPageUrl(web, settings));
        }
        catch
        {
            boardHost.Dispose();
            throw;
        }
    }

    public Task WaitForShutdownAsync()
    {
        return web.WaitForShutdownAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await web.StopAsync();
        await web.DisposeAsync();
        boardHost.Dispose();
    }

    private static WebApplication BuildWebApplication(VirtualBoardSettings settings, InMemoryMacroBoard board, string? boardEndpoint)
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Only the address from the settings counts. Addresses from the environment
        // (ASPNETCORE_URLS and similar) would otherwise cause a warning at the start.
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, string.Empty);
        builder.WebHost.UseSetting(WebHostDefaults.HttpPortsKey, string.Empty);
        builder.WebHost.UseSetting(WebHostDefaults.HttpsPortsKey, string.Empty);
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(settings.BindAddress, settings.WebPort));

        var web = builder.Build();
        var viewerKeys = new ViewerKeys(board);

        web.Use(RequestGuard.RejectForeignRequestsAsync);
        web.UseWebSockets();

        web.MapGet("/", () => EmbeddedFile("index.html", "text/html; charset=utf-8"));
        web.MapGet("/glassKey.png", () => EmbeddedFile("glassKey.png", "image/png"));
        web.MapGet("/favicon.ico", () => EmbeddedFile("favicon.ico", "image/x-icon"));

        web.Map("/ws", async (HttpContext context, IHostApplicationLifetime lifetime) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            using var viewer = new ViewerConnection(socket, board, viewerKeys);

            var boardMessage = ViewerProtocol.BoardMessage(settings, boardEndpoint, board.Brightness);
            await viewer.RunAsync(boardMessage, lifetime.ApplicationStopping);
        });

        return web;
    }

    private static IResult EmbeddedFile(string name, string contentType)
    {
        var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("OpenMacroBoard.VirtualBoard.Web." + name);

        return stream is null
            ? Results.NotFound()
            : Results.Stream(stream, contentType);
    }

    private static Uri GetPageUrl(WebApplication web, VirtualBoardSettings settings)
    {
        // With port zero the operating system chooses the port, the server knows which one.
        var address = web.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses
            .FirstOrDefault();

        var port = address is null ? settings.WebPort : new Uri(address).Port;

        // "All addresses" is not an address a browser can open, the local one works then.
        var host = settings.BindAddress.Equals(IPAddress.Any) || settings.BindAddress.Equals(IPAddress.IPv6Any)
            ? IPAddress.Loopback
            : settings.BindAddress;

        return new UriBuilder("http", host.ToString(), port).Uri;
    }
}
