
![StreamDeckSharp Banner](https://raw.githubusercontent.com/OpenStreamDeck/StreamDeckSharp/master/doc/images/banner/StreamDeckSharpBanner_150px.png)

-----------------

**StreamDeckSharp is a simple (unofficial) library (.NET 10+) for the [Elgato Stream Deck](https://www.elgato.com/en/gaming/stream-deck) family**

[![license](https://img.shields.io/github/license/OpenStreamDeck/StreamDeckSharp.svg)](https://github.com/OpenStreamDeck/StreamDeckSharp/blob/master/LICENSE.md)
[![GitHub release](https://img.shields.io/github/release/OpenStreamDeck/StreamDeckSharp.svg)](https://github.com/OpenStreamDeck/StreamDeckSharp/releases)
[![Nuget version](https://img.shields.io/nuget/v/streamdecksharp.svg)](https://www.nuget.org/packages/StreamDeckSharp/)
[![AI:W WRITE](https://img.shields.io/badge/AI-WRITE-grey?style=flat&logoSize=auto&labelColor=13171f)](AI-DISCLOSURE.md)

First things first, StreamDeckSharp is not official software by Elgato, nor is it endorsed by them.

StreamDeckSharp is a device provider implementation for [`OpenMacroBoard.SDK`](https://github.com/OpenMacroBoard/OpenMacroBoard.SDK).

# Quick Start

Create a new console project in Visual Studio (.NET 10+), add [`OpenMacroBoard.SDK`](https://www.nuget.org/packages/OpenMacroBoard.SDK/) as a reference and at least one device provider.

In this example we use

- [`OpenMacroBoard.SocketIO`](https://www.nuget.org/packages/OpenMacroBoard.SocketIO/) to support the `VirtualMacroBoard`
- [`StreamDeckSharp`](https://www.nuget.org/packages/StreamDeckSharp/) to support the [Elgato Stream Deck family](https://www.elgato.com/de/de/s/welcome-to-stream-deck)  
  <sub>_**Note**: Neither OpenMacroBoard nor StreamDeckSharp are made or endorsed by Elgato_</sub>

Once you added the NuGet packages copy-paste the following lines:

```csharp
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;  // for VirtualMacroBoard
using StreamDeckSharp;          // for StreamDeck

// create a device context (fluent API)
// and add listener for devices (device provider)
using var ctx = DeviceContext.Create()
    .AddListener<SocketIOBoardListener>()   // VirtualMacroBoard
    .AddListener<StreamDeckListener>()      // StreamDeck
    ;

Console.WriteLine("Waiting for a device... (press Ctrl+C to cancel)");
using var board = await ctx.OpenAsync();
Console.WriteLine("Device found.");
Console.WriteLine("1) Try to press some buttons on the device.");
Console.WriteLine("2) Press any key in this console to end the demo.");

// react to key press event by setting a random color
board.KeyStateChanged += (sender, arg) => board.SetKeyBitmap(arg.Key, GetRandomColorKey());

// Wait for a key press in the console window to exit
// the application and disconnect the device.
Console.ReadKey();

// Helper function to create a random color KeyBitmap
static KeyBitmap GetRandomColorKey()
{
    var r = GetRandomByte();
    var g = GetRandomByte();
    var b = GetRandomByte();

    return KeyBitmap.Create.FromRgb(r, g, b);
}

// Helper function to get a random byte
static byte GetRandomByte()
{
    return (byte)Random.Shared.Next(255);
}
```

Here is what the example looks like after pressing some keys:
![](https://raw.githubusercontent.com/OpenMacroBoard/StreamDeckSharp/refs/heads/main/doc/images/rainbow_example.png)

# Supported devices

NuGet: [`StreamDeckSharp`](https://www.nuget.org/packages/StreamDeckSharp/)

| Device                                                                | Description |
| --------------------------------------------------------------------- | ----------- |
| Stream Deck _(original/legacy)_                                       | 5 x 3       |
| [Stream Deck](https://www.elgato.com/de/gaming/stream-deck) _(MK2)_   | 5 x 3       |
| [Stream Deck XL](https://www.elgato.com/ww/de/p/stream-deck-xl)       | 8 x 4       |
| [Stream Deck Mini](https://www.elgato.com/de/gaming/stream-deck-mini) | 3 x 2       |

Keep in mind that Elgato sometimes releases new revisions of their devices with different PIDs (USB product IDs) which might break compatibility. If you have a device like that, please open an issue on GitHub with the new PID.

# Examples

You can find a lot of examples in our [example collection](https://github.com/OpenMacroBoard/OpenMacroBoard.ExampleCollection)


## Fullscreen images
<img src="https://raw.githubusercontent.com/OpenMacroBoard/openmacroboard.github.io/refs/heads/main/assets/images/lasershow.png" width="500" />

## Play games
Play games on a macro board, for example minesweeper (also part of the example projects)
<img src="https://raw.githubusercontent.com/OpenMacroBoard/StreamDeckSharp/main/doc/images/minesweeper.jpg" width="500" />

## Videos
[![Demo video of the example](https://i.imgur.com/8tlkaIg.png)](http://www.youtube.com/watch?v=tNwUG0sPmKw)  
_\*The glitches you can see are already fixed._



---
 
###### This project is not related to *Elgato Systems GmbH* in any way

---