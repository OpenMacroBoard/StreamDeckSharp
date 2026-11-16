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
