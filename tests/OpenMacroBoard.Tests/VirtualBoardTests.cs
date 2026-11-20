using AwesomeAssertions;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using OpenMacroBoard.VirtualBoard;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests;

/// <summary>
/// Runs the virtual board in the test process and talks to it like its two kinds of clients:
/// an application through SocketIO, and a browser page through the WebSocket.
/// </summary>
public class VirtualBoardTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ViewerGetsTheBoardDescriptionFirstAndThenTheLogo()
    {
        await using var app = await StartBoardAsync();
        using var viewer = await Viewer.ConnectAsync(app);

        var first = await viewer.ReceiveAsync();

        first.Json.Should().NotBeNull();
        first.Json.RootElement.GetProperty("type").GetString().Should().Be("board");
        first.Json.RootElement.GetProperty("deviceName").GetString().Should().Be("Test Board");
        first.Json.RootElement.GetProperty("endpoint").GetString().Should().Be(app.BoardEndpoint.ToString());
        first.Json.RootElement.GetProperty("countX").GetInt32().Should().Be(3);
        first.Json.RootElement.GetProperty("countY").GetInt32().Should().Be(2);
        first.Json.RootElement.GetProperty("keySize").GetInt32().Should().Be(16);
        first.Json.RootElement.GetProperty("gapSize").GetInt32().Should().Be(4);

        // Every key arrives once. The board shows the logo, so not everything is black.
        var images = new Dictionary<int, Message>();

        while (images.Count < 6)
        {
            var message = await viewer.ReceiveAsync();

            if (message.Json is null)
            {
                images[message.KeyId] = message;
            }
        }

        images.Values.Should().Contain(image => image.Width == 16 && image.Height == 16 && !IsBlack(image.Rgba));
    }

    [Fact]
    public async Task ImageFromAnApplicationReachesTheViewer()
    {
        await using var app = await StartBoardAsync();
        using var viewer = await Viewer.ConnectAsync(app);
        using var board = OpenThroughSocketIO(app);

        board.SetKeyBitmap(4, KeyBitmap.Create.FromRgb(200, 100, 50));

        var image = await viewer.ReceiveUntilAsync(m => m.Json is null && m.KeyId == 4 && m.Width > 0 && m.Rgba[0] == 200);

        image.Rgba.AsSpan(0, 4).ToArray().Should().Equal(200, 100, 50, 255);
        image.Rgba.Should().HaveCount(image.Width * image.Height * 4);

        // The first image of an application replaces the logo, the other keys turn black.
        app.Board.GetKeyImage(0).Should().Be(KeyBitmap.Black);
    }

    [Fact]
    public async Task BrightnessFromAnApplicationReachesTheViewer()
    {
        await using var app = await StartBoardAsync();
        using var viewer = await Viewer.ConnectAsync(app);
        using var board = OpenThroughSocketIO(app);

        board.SetBrightness(30);

        var message = await viewer.ReceiveUntilAsync(
            m => m.Json?.RootElement.GetProperty("type").GetString() == "brightness"
                && m.Json.RootElement.GetProperty("percent").GetInt32() == 30
        );

        message.Should().NotBeNull();
    }

    [Fact]
    public async Task KeyOfAViewerReachesTheApplicationAndOtherViewers()
    {
        await using var app = await StartBoardAsync();
        using var presser = await Viewer.ConnectAsync(app);
        using var watcher = await Viewer.ConnectAsync(app);
        using var board = OpenThroughSocketIO(app);

        var events = new BlockingCollection<KeyEventArgs>();
        board.KeyStateChanged += (_, e) => events.Add(e);
        await WaitForAsync(() => app.Board.HasKeyStateSubscribers);

        await presser.SendAsync("""{"type":"key","key":2,"down":true}""");

        Take(events).Should().BeEquivalentTo(new { Key = 2, IsDown = true });

        var seen = await watcher.ReceiveUntilAsync(m => m.Json?.RootElement.GetProperty("type").GetString() == "keyState");
        seen.Json.RootElement.GetProperty("key").GetInt32().Should().Be(2);
        seen.Json.RootElement.GetProperty("down").GetBoolean().Should().BeTrue();

        await presser.SendAsync("""{"type":"key","key":2,"down":false}""");

        Take(events).Should().BeEquivalentTo(new { Key = 2, IsDown = false });
    }

    [Fact]
    public async Task KeyStaysDownWhileAnyViewerHoldsItAndIsReleasedWhenThatViewerDisconnects()
    {
        await using var app = await StartBoardAsync();
        using var first = await Viewer.ConnectAsync(app);
        var second = await Viewer.ConnectAsync(app);

        var events = new BlockingCollection<KeyEventArgs>();
        app.Board.KeyStateChanged += (_, e) => events.Add(e);

        await first.SendAsync("""{"type":"key","key":1,"down":true}""");
        await second.SendAsync("""{"type":"key","key":1,"down":true}""");
        await WaitForAsync(() => app.Board.IsKeyDown(1));

        await first.SendAsync("""{"type":"key","key":1,"down":false}""");

        // Something else that is processed after the release, to know the release was handled.
        await first.SendAsync("""{"type":"key","key":0,"down":true}""");
        await WaitForAsync(() => app.Board.IsKeyDown(0));

        app.Board.IsKeyDown(1).Should().BeTrue("the second viewer still holds the key");

        second.Dispose();
        await WaitForAsync(() => !app.Board.IsKeyDown(1));

        events.Should().BeEquivalentTo(
            [
                new { Key = 1, IsDown = true },
                new { Key = 0, IsDown = true },
                new { Key = 1, IsDown = false },
            ],
            options => options.WithStrictOrdering()
        );
    }

    [Fact]
    public async Task InvalidMessagesOfAViewerAreIgnored()
    {
        await using var app = await StartBoardAsync();
        using var viewer = await Viewer.ConnectAsync(app);

        await viewer.SendAsync("not json");
        await viewer.SendAsync("""{"type":"key","key":99,"down":true}""");
        await viewer.SendAsync("""{"type":"key","key":"1","down":true}""");
        await viewer.SendAsync("""{"type":"key","key":1,"down":"yes"}""");
        await viewer.SendAsync(new string('x', 5000));
        await viewer.SendAsync("""{"type":"key","key":5,"down":true}""");

        await WaitForAsync(() => app.Board.IsKeyDown(5));

        for (var keyId = 0; keyId < 5; keyId++)
        {
            app.Board.IsKeyDown(keyId).Should().BeFalse();
        }
    }

    [Fact]
    public async Task PageIsServedAndRequestsOfOtherWebsitesAreRefused()
    {
        await using var app = await StartBoardAsync();
        using var http = new HttpClient { BaseAddress = app.PageUrl, Timeout = Timeout };
        var cancellationToken = TestContext.Current.CancellationToken;

        var page = await http.GetStringAsync("/", cancellationToken);
        page.Should().Contain("<canvas");

        using var glass = await http.GetAsync("/glassKey.png", cancellationToken);
        glass.StatusCode.Should().Be(HttpStatusCode.OK);

        using var foreignOrigin = new HttpRequestMessage(HttpMethod.Get, "/ws");
        foreignOrigin.Headers.Add("Origin", "http://other.example");
        using var foreignOriginResponse = await http.SendAsync(foreignOrigin, cancellationToken);
        foreignOriginResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var foreignHost = new HttpRequestMessage(HttpMethod.Get, "/");
        foreignHost.Headers.Host = "other.example";
        using var foreignHostResponse = await http.SendAsync(foreignHost, cancellationToken);
        foreignHostResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("--keysx", "0")]
    [InlineData("--keysize", "0")]
    [InlineData("--keygap", "-1")]
    [InlineData("--binding", "not-an-address")]
    [InlineData("--port", "65000")]
    public void InvalidSettingsAreRefused(string option, string value)
    {
        var options = new CommandLineOptions();

        switch (option)
        {
            case "--keysx":
                options.KeysX = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "--keysize":
                options.KeySize = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "--keygap":
                options.KeyGap = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "--binding":
                options.BindingIpAddress = value;
                break;
            default:
                // The page would get port 66000, which does not exist.
                options.Port = ushort.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                break;
        }

        Program.TryCreateSettings(options, out _, out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
    }

    [Fact]
    public void DefaultSettingsMatchTheOldVirtualBoard()
    {
        Program.TryCreateSettings(new CommandLineOptions(), out var settings, out _).Should().BeTrue();

        settings.DeviceName.Should().Be("Virtual Board - 5x3");
        settings.BindAddress.Should().Be(IPAddress.Loopback);
        settings.BoardPort.Should().Be(27621);
        settings.WebPort.Should().Be(28621);
        settings.KeySize.Should().Be(72);
        settings.KeyGap.Should().Be(25);
        settings.ShowBoardEndpoint.Should().BeTrue();
    }

    private static Task<VirtualBoardApp> StartBoardAsync()
    {
        // Port zero: the operating system chooses free ports, so tests can run side by side.
        return VirtualBoardApp.StartAsync(new VirtualBoardSettings
        {
            DeviceName = "Test Board",
            BoardPort = 0,
            WebPort = 0,
            KeysX = 3,
            KeysY = 2,
            KeySize = 16,
            KeyGap = 4,
        });
    }

    private static IMacroBoard OpenThroughSocketIO(VirtualBoardApp app)
    {
        var layout = new GridKeyLayout(3, 2, 16, 4);
        return new SocketIODeviceReference("Test Board", app.BoardEndpoint, layout).Open();
    }

    private static KeyEventArgs Take(BlockingCollection<KeyEventArgs> events)
    {
        events.TryTake(out var item, Timeout).Should().BeTrue("a key event was expected");
        return item;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var end = DateTime.UtcNow + Timeout;

        while (!condition())
        {
            (DateTime.UtcNow < end).Should().BeTrue("the expected state was not reached in time");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private static bool IsBlack(byte[] rgba)
    {
        for (var i = 0; i < rgba.Length; i += 4)
        {
            if (rgba[i] != 0 || rgba[i + 1] != 0 || rgba[i + 2] != 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A message of the server: either JSON, or a key image.
    /// </summary>
    private sealed record Message(JsonDocument Json, int KeyId, int Width, int Height, byte[] Rgba);

    /// <summary>
    /// What the browser page is for the server: a WebSocket client.
    /// </summary>
    private sealed class Viewer : IDisposable
    {
        private readonly ClientWebSocket socket = new();

        public static async Task<Viewer> ConnectAsync(VirtualBoardApp app)
        {
            var viewer = new Viewer();
            var url = new UriBuilder(app.PageUrl) { Scheme = "ws", Path = "/ws" }.Uri;

            using var timeout = new CancellationTokenSource(Timeout);
            await viewer.socket.ConnectAsync(url, timeout.Token);

            return viewer;
        }

        public async Task SendAsync(string text)
        {
            using var timeout = new CancellationTokenSource(Timeout);
            await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, timeout.Token);
        }

        public async Task<Message> ReceiveUntilAsync(Func<Message, bool> isWanted)
        {
            while (true)
            {
                var message = await ReceiveAsync();

                if (isWanted(message))
                {
                    return message;
                }
            }
        }

        public async Task<Message> ReceiveAsync()
        {
            using var timeout = new CancellationTokenSource(Timeout);
            using var content = new MemoryStream();
            var buffer = new byte[16 * 1024];

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer.AsMemory(), timeout.Token);
                await content.WriteAsync(buffer.AsMemory(0, result.Count), timeout.Token);

                if (!result.EndOfMessage)
                {
                    continue;
                }

                var bytes = content.ToArray();

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    return new Message(JsonDocument.Parse(bytes), -1, 0, 0, []);
                }

                bytes[0].Should().Be(1, "key images are the only binary messages");

                return new Message(
                    null,
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(1)),
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(3)),
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(5)),
                    bytes[7..]
                );
            }
        }

        public void Dispose()
        {
            // Like a closed browser tab: the connection just ends.
            socket.Abort();
            socket.Dispose();
        }
    }
}
