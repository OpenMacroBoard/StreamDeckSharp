## What is OpenMacroBoard?

OpenMacroBoard (maintained by [Christian Franzl](https://github.com/wischi-chr))
is a collection of .NET libraries (.NET 10+),
that help you implement custom functionality for various macro boards
mainly focusing on devices with key displays.

The OpenMacroBoard.SDK defines the common base types and logic for macro boards.
The SDK by itself is not very useful on its own, because it doesn't implement any concrete devices.

The concrete device implementations (device providers) are in different libraries.

## Quick Start

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

## Providers and supported devices

Providers are libraries that manage the communication to the macro boards.
This abstraction is needed to allow third parties to implement devices without changes to the core functionality.

At the moment there are just two providers that are maintained by me.


### StreamDeckSharp

First things first, StreamDeckSharp is not official software by Elgato, nor is it endorsed by them.

NuGet: [`StreamDeckSharp`](https://www.nuget.org/packages/StreamDeckSharp/)

| Device                                                                | Description |
| --------------------------------------------------------------------- | ----------- |
| Stream Deck _(original/legacy)_                                       | 5 x 3       |
| [Stream Deck](https://www.elgato.com/de/gaming/stream-deck) _(MK2)_   | 5 x 3       |
| [Stream Deck XL](https://www.elgato.com/ww/de/p/stream-deck-xl)       | 8 x 4       |
| [Stream Deck Mini](https://www.elgato.com/de/gaming/stream-deck-mini) | 3 x 2       |

Keep in mind that Elgato sometimes releases new revisions of their devices with different PIDs (USB product IDs) which might break compatibility. If you have a device like that, please open an issue on GitHub with the new PID.

### OpenMacroBoard.SocketIO 

NuGet: [`OpenMacroBoard.SocketIO`](https://www.nuget.org/packages/OpenMacroBoard.SocketIO/)

| Device       | Description                                              |
| ------------ | -------------------------------------------------------- |
| VirtualBoard | Software emulated board with arbitrary key configuration |


## Is device _XYZ_ supported?

If I find the time I'd love to add more, but you can also implement one yourself by referencing `OpenMacroBoard.SDK` and writing a class that implements `IObservable<DeviceStateReport>`. This class can then be added as a device listener in a `DeviceContext`.
If you want me to implement it, you can donate hardware (or the money so I can buy that specific hardware you want implemented) - just create a ticket and we talk about it ;-)

## Examples

You can find a lot of examples in our [example collection](https://github.com/OpenMacroBoard/OpenMacroBoard.ExampleCollection)


### Fullscreen images
<img src="https://raw.githubusercontent.com/OpenMacroBoard/openmacroboard.github.io/refs/heads/main/assets/images/lasershow.png" width="500" />

### Play games
Play games on a macro board, for example minesweeper (also part of the example projects)
<img src="https://raw.githubusercontent.com/OpenMacroBoard/StreamDeckSharp/main/doc/images/minesweeper.jpg" width="500" />

### Videos
[![Demo video of the example](https://i.imgur.com/8tlkaIg.png)](http://www.youtube.com/watch?v=tNwUG0sPmKw)  
_\*The glitches you can see are already fixed._

