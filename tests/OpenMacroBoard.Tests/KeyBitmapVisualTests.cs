using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests;

public class KeyBitmapVisualTests
{
    public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

    [Theory]
    [ClassData(typeof(DeviceGridKeyPositionDataProvider))]
    public async Task FakeMacroBoardSelfCheck(string deviceName, GridKeyLayout keys)
    {
        Verifier.Initialize();

        Verifier
            .UseFileNameAsDirectory()
            .UseFileName(deviceName)
            ;

        using var board = new FakeMacroBoard(keys);
        int stateCnt = 0;

        Task VerifyBoardStateAsync(string stateName)
        {
            var currentCount = stateCnt;
            stateCnt++;

            return Verifier
                .WithFileNameSuffix($"_{currentCount:D3}")
                .WithFileNameSuffix($"_{stateName}")
                .VerifyAsync(board);
        }

        await VerifyBoardStateAsync("AfterConstruction");

        var red = KeyBitmap.Create.FromColor(OmbColor.Red);
        board.SetKeyBitmap(red);

        await VerifyBoardStateAsync("EverythingRed");

        board.ClearKeys();

        await VerifyBoardStateAsync("BoardCleared");

        using var fullImg = AssetLoader.LoadImage("ExampleImageText.jpg");
        board.DrawFullScreenBitmap(fullImg);

        await VerifyBoardStateAsync("FullscreenDrawn");

        board.ShowLogo();

        await VerifyBoardStateAsync("ShowLogo");

        var blue = KeyBitmap.Create.FromColor(OmbColor.AliceBlue);
        board.SetKeyBitmap(2, blue);

        await VerifyBoardStateAsync("SingleKeyAfterLogo");

        board.IsConnected = false;

        await VerifyBoardStateAsync("DisconnectState");

        // send key in disconnected state should do nothing.
        board.SetKeyBitmap(3, blue);

        await VerifyBoardStateAsync("DisconnectStateSetBitmap");

        board.IsConnected = true;

        await VerifyBoardStateAsync("ReconnectedState");
    }

    [Fact]
    public async Task ButtonPressEffectWorkingAsExpected()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory();

        var underlyingBoard = new FakeMacroBoard(DeviceGridKeyPositionDataProvider.Medium);
        using var board = underlyingBoard.WithButtonPressEffect();

        var keyImage = new Image<Bgr24>(72, 72);

        keyImage.Mutate(x =>
        {
            var circle = new EllipsePolygon(36, 36, 20);
            x.Fill(Color.Green, circle);
        });

        var key = KeyBitmap.Create.FromImageSharpImage(keyImage);

        board.SetKeyBitmap(7, key);

        await Verifier.WithFileName("0.BitmapSet").VerifyAsync(underlyingBoard);

        underlyingBoard.TriggerKeyEvent(7, true);

        await Verifier.WithFileName("1.KeyPressed").VerifyAsync(underlyingBoard);

        underlyingBoard.TriggerKeyEvent(7, false);

        await Verifier.WithFileName("2.KeyReleased").VerifyAsync(underlyingBoard);
    }

    [Fact]
    public async Task DisconnectReplayAdapterWorksAsExpected()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory();

        var underlyingBoard = new FakeMacroBoard(DeviceGridKeyPositionDataProvider.Medium);
        using var board = underlyingBoard.WithDisconnectReplay();

        var key = KeyBitmap.Create.FromColor(OmbColor.Magenta);

        board.SetBrightness(70);
        board.SetKeyBitmap(7, key);

        await Verifier.WithFileName("0.StateApplied").VerifyAsync(underlyingBoard);

        underlyingBoard.IsConnected = false;

        await Verifier.WithFileName("1.Disconnected").VerifyAsync(underlyingBoard);

        underlyingBoard.IsConnected = true;

        await Verifier.WithFileName("2.Reconnected").VerifyAsync(underlyingBoard);
    }

    [Fact]
    public async Task BitmapKeyChannelsWorkAsExpectedForBgr24()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory();

        using var board = new FakeMacroBoard(DeviceGridKeyPositionDataProvider.Medium);

        var width = 72;
        var height = 72;
        var channels = 3;
        var pixelCount = width * height;
        var pixelDataLength = pixelCount * channels;

        var bKeyArray = new byte[pixelDataLength];
        var gKeyArray = new byte[pixelDataLength];
        var rKeyArray = new byte[pixelDataLength];

        for (int i = 0; i < pixelDataLength; i += channels)
        {
            bKeyArray[i + 0] = 255;
            gKeyArray[i + 1] = 255;
            rKeyArray[i + 2] = 255;
        }

        var bKey = KeyBitmap.Create.FromBgr24Array(width, height, bKeyArray);
        var gKey = KeyBitmap.Create.FromBgr24Array(width, height, gKeyArray);
        var rKey = KeyBitmap.Create.FromBgr24Array(width, height, rKeyArray);

        board.SetBrightness(100);

        int cnt = 0;

        async Task TestKey(KeyBitmap key, string state)
        {
            board.SetKeyBitmap(7, key);
            var fileName = $"{cnt}.State_24_{state}";
            await Verifier.WithFileName(fileName).VerifyAsync(board);

            cnt++;
        }

        await TestKey(bKey, "B");
        await TestKey(gKey, "G");
        await TestKey(rKey, "R");
    }

    [Fact]
    public async Task BitmapKeyChannelsWorkAsExpectedFor32bitExtensions()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory();

        using var board = new FakeMacroBoard(DeviceGridKeyPositionDataProvider.Medium);

        var width = 72;
        var height = 72;
        var channels = 4;
        var pixelCount = width * height;
        var pixelDataLength = pixelCount * channels;

        var bgra32BKeyArray = new byte[pixelDataLength];
        var bgra32GKeyArray = new byte[pixelDataLength];
        var bgra32RKeyArray = new byte[pixelDataLength];

        var rgba32RKeyArray = new byte[pixelDataLength];
        var rgba32GKeyArray = new byte[pixelDataLength];
        var rgba32BKeyArray = new byte[pixelDataLength];

        for (int i = 0; i < pixelDataLength; i += channels)
        {
            bgra32BKeyArray[i + 0] = 255;
            bgra32GKeyArray[i + 1] = 255;
            bgra32RKeyArray[i + 2] = 255;

            rgba32RKeyArray[i + 0] = 255;
            rgba32GKeyArray[i + 1] = 255;
            rgba32BKeyArray[i + 2] = 255;
        }

        var b1Key = KeyBitmap.Create.FromBgra32Array(width, height, bgra32BKeyArray);
        var g1Key = KeyBitmap.Create.FromBgra32Array(width, height, bgra32GKeyArray);
        var r1Key = KeyBitmap.Create.FromBgra32Array(width, height, bgra32RKeyArray);

        var b2Key = KeyBitmap.Create.FromRgba32Array(width, height, rgba32BKeyArray);
        var g2Key = KeyBitmap.Create.FromRgba32Array(width, height, rgba32GKeyArray);
        var r2Key = KeyBitmap.Create.FromRgba32Array(width, height, rgba32RKeyArray);

        board.SetBrightness(100);

        int cnt = 0;

        async Task TestKey(KeyBitmap key, string state)
        {
            board.SetKeyBitmap(7, key);
            var fileName = $"{cnt}.State_32_{state}";
            await Verifier.WithFileName(fileName).VerifyAsync(board);

            cnt++;
        }

        await TestKey(b1Key, "B1");
        await TestKey(g1Key, "G1");
        await TestKey(r1Key, "R1");

        await TestKey(b2Key, "B2");
        await TestKey(g2Key, "G2");
        await TestKey(r2Key, "R2");
    }
}
