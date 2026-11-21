# OpenMacroBoard.SocketIO

A device provider for [OpenMacroBoard](../OpenMacroBoard.SDK) that reaches macro boards over
the network instead of USB. Its main use is the [virtual board](../OpenMacroBoard.VirtualBoard),
a board in the browser for developing without hardware.

The name is historic: this is the package's own small protocol over TCP and UDP, and has
nothing to do with the JavaScript library socket.io.

## Use a board in your application

Add the listener to your device context. Boards that run on the same machine are found
automatically and behave like any other macro board.

```csharp
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;

using var ctx = DeviceContext.Create()
    .AddListener<SocketIOBoardListener>();

using var board = await ctx.OpenAsync();
board.SetKeyBitmap(0, KeyBitmap.Create.FromRgb(255, 0, 0));
```

To try it, start the virtual board from a clone of the repository:

```
dotnet run --project src/OpenMacroBoard.VirtualBoard
```

## Host a board yourself

`SocketIOMacroBoardHost` makes any `IMacroBoard` available to other applications. That can be
a real device you want to share with another process, or your own implementation.

```csharp
using OpenMacroBoard.SocketIO;
using System.Net;

// "board" is the IMacroBoard to share. Port 0 lets the operating system choose a free port.
using var host = new SocketIOMacroBoardHost(board, "My board", IPAddress.Loopback);
Console.WriteLine($"Hosting on {host.HostEndpoint}");
```

## Network and security

There is **no authentication and no encryption**. Everybody who can reach a hosted board can
show images on it and receive its key presses.

For that reason the listener and the host both use the loopback address `127.0.0.1` by default,
so boards are only visible on the same machine. You can pass another address to their
constructors to reach boards on other machines, but only do that in a network you trust.

Boards are discovered with UDP broadcasts on port 35891. Each board then listens on its own
TCP port.
