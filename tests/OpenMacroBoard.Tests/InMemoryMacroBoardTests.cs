using AwesomeAssertions;
using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using Xunit;

namespace OpenMacroBoard.Tests;

public class InMemoryMacroBoardTests
{
    private static readonly KeyBitmap Red = KeyBitmap.Create.FromRgb(255, 0, 0);

    [Fact]
    public void StartsConnectedWithLogoBlackKeysAndFullBrightness()
    {
        using var board = CreateBoard();

        board.IsConnected.Should().BeTrue();
        board.IsLogoShown.Should().BeTrue();
        board.IsDisposed.Should().BeFalse();
        board.Brightness.Should().Be(100);
        board.Keys.Count.Should().Be(6);
        board.GetFirmwareVersion().Should().BeEmpty();
        board.GetSerialNumber().Should().BeEmpty();

        for (var keyId = 0; keyId < 6; keyId++)
        {
            board.GetKeyBitmap(keyId).Should().BeSameAs(KeyBitmap.Black);
            board.IsKeyDown(keyId).Should().BeFalse();
        }
    }

    [Fact]
    public void RemembersTheImageOfEachKeyAndTellsAboutIt()
    {
        using var board = CreateBoard();
        var changed = new List<int>();
        board.KeyBitmapChanged += (_, e) => changed.Add(e.Key);

        board.SetKeyBitmap(4, Red);

        board.GetKeyBitmap(4).Should().BeSameAs(Red);
        board.GetKeyBitmap(3).Should().BeSameAs(KeyBitmap.Black);
        board.IsLogoShown.Should().BeFalse();
        changed.Should().Equal(4);
    }

    [Fact]
    public void NullImageMakesTheKeyBlack()
    {
        using var board = CreateBoard();
        board.SetKeyBitmap(1, Red);

        board.SetKeyBitmap(1, null!);

        board.GetKeyBitmap(1).Should().BeSameAs(KeyBitmap.Black);
    }

    [Fact]
    public void ExtensionMethodsOfTheSdkWorkOnIt()
    {
        using var board = CreateBoard();

        board.SetKeyBitmap(Red);

        for (var keyId = 0; keyId < 6; keyId++)
        {
            board.GetKeyBitmap(keyId).Should().BeSameAs(Red);
        }

        board.ClearKeys();
        board.GetKeyBitmap(5).Should().BeSameAs(KeyBitmap.Black);
    }

    [Fact]
    public void ShowLogoMakesAllKeysBlackUntilTheNextImage()
    {
        using var board = CreateBoard();
        board.SetKeyBitmap(0, Red);
        var changed = new List<int>();
        board.KeyBitmapChanged += (_, e) => changed.Add(e.Key);

        board.ShowLogo();

        board.IsLogoShown.Should().BeTrue();
        board.GetKeyBitmap(0).Should().BeSameAs(KeyBitmap.Black);
        changed.Should().Equal(0, 1, 2, 3, 4, 5);

        board.SetKeyBitmap(2, Red);
        board.IsLogoShown.Should().BeFalse();
    }

    [Fact]
    public void BrightnessIsRememberedAndLimitedTo100()
    {
        using var board = CreateBoard();
        var events = 0;
        board.BrightnessChanged += (_, _) => events++;

        board.SetBrightness(37);
        board.Brightness.Should().Be(37);

        board.SetBrightness(200);
        board.Brightness.Should().Be(100);

        events.Should().Be(2);
    }

    [Fact]
    public void PressingAKeyRaisesTheEventOncePerChange()
    {
        using var board = CreateBoard();
        var events = new List<(object Sender, int Key, bool IsDown)>();
        board.KeyStateChanged += (sender, e) => events.Add((sender!, e.Key, e.IsDown));

        board.SetKeyState(3, isDown: true);
        board.SetKeyState(3, isDown: true);
        board.IsKeyDown(3).Should().BeTrue();

        board.SetKeyState(3, isDown: false);
        board.SetKeyState(3, isDown: false);
        board.IsKeyDown(3).Should().BeFalse();

        events.Should().Equal((board, 3, true), (board, 3, false));
    }

    [Fact]
    public void WhileNotConnectedEverythingIsIgnored()
    {
        using var board = CreateBoard();
        board.SetKeyBitmap(0, Red);
        board.SetBrightness(50);
        board.SetKeyState(1, isDown: true);

        var connectionEvents = new List<bool>();
        var keyEvents = 0;
        board.ConnectionStateChanged += (_, e) => connectionEvents.Add(e.NewConnectionState);
        board.KeyStateChanged += (_, _) => keyEvents++;

        board.IsConnected = false;
        board.IsConnected = false;

        board.IsConnected.Should().BeFalse();
        board.IsKeyDown(1).Should().BeFalse("a device that is unplugged holds no keys");

        board.SetKeyBitmap(0, KeyBitmap.Black);
        board.SetBrightness(10);
        board.ShowLogo();
        board.SetKeyState(2, isDown: true);

        board.GetKeyBitmap(0).Should().BeSameAs(Red);
        board.Brightness.Should().Be(50);
        board.IsLogoShown.Should().BeFalse();
        board.IsKeyDown(2).Should().BeFalse();
        keyEvents.Should().Be(0);

        board.IsConnected = true;

        board.IsLogoShown.Should().BeTrue("a device shows its logo when it is plugged in");
        board.GetKeyBitmap(0).Should().BeSameAs(KeyBitmap.Black);
        connectionEvents.Should().Equal(false, true);
    }

    [Fact]
    public void FirmwareVersionAndSerialNumberCanBeChosen()
    {
        using var board = new InMemoryMacroBoard(new GridKeyLayout(1, 1, 72, 10))
        {
            FirmwareVersion = "1.2.3",
            SerialNumber = "ABC",
        };

        board.GetFirmwareVersion().Should().Be("1.2.3");
        board.GetSerialNumber().Should().Be("ABC");
    }

    [Fact]
    public void AfterDisposeSendingThrowsButReadingWorks()
    {
        var board = CreateBoard();
        board.SetKeyBitmap(0, Red);

        board.Dispose();
        board.Dispose();

        board.IsDisposed.Should().BeTrue();
        board.GetKeyBitmap(0).Should().BeSameAs(Red);

        ((Action)(() => board.SetKeyBitmap(0, Red))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => board.SetBrightness(1))).Should().Throw<ObjectDisposedException>();
        ((Action)board.ShowLogo).Should().Throw<ObjectDisposedException>();

        board.SetKeyState(0, isDown: true);
        board.IsKeyDown(0).Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void KeysTheBoardDoesNotHaveAreRejected(int keyId)
    {
        using var board = CreateBoard();

        ((Action)(() => board.SetKeyBitmap(keyId, Red))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => board.GetKeyBitmap(keyId))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => board.IsKeyDown(keyId))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => board.SetKeyState(keyId, isDown: true))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AnApplicationCanBeTestedWithIt()
    {
        // The kind of test this class is for: the "application" lights a key while it is held.
        using var board = CreateBoard();
        IMacroBoard seenByTheApplication = board;

        seenByTheApplication.KeyStateChanged += (sender, e) =>
            ((IMacroBoard)sender!).SetKeyBitmap(e.Key, e.IsDown ? Red : KeyBitmap.Black);

        board.SetKeyState(2, isDown: true);
        board.GetKeyBitmap(2).Should().BeSameAs(Red);

        board.SetKeyState(2, isDown: false);
        board.GetKeyBitmap(2).Should().BeSameAs(KeyBitmap.Black);
    }

    private static InMemoryMacroBoard CreateBoard()
    {
        return new InMemoryMacroBoard(new GridKeyLayout(3, 2, 72, 10));
    }
}
