using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using StreamDeckSharp;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace OpenMacroBoard.Examples.DeviceContextShowcase;

[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "Just an example")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Just an example")]
internal static class Program
{
    private static void Main()
    {
        // You can try different options:

        RunExampleDeviceList();

        // RunExampleEventLog();
    }

    public static void RunExampleDeviceList()
    {
        using var ctx = DeviceContext.Create()
            .AddListener<StreamDeckListener>()
            .AddListener(new SocketIOBoardListener(IPAddress.Loopback))
            ;

        using var sub = ctx.DeviceStateReports.Subscribe(_ => RedrawDeviceList(ctx));

        RedrawDeviceList(ctx);

        Console.ReadKey();
    }

    public static void RunExampleEventLog()
    {
        using var ctx = DeviceContext.Create().AddListener<StreamDeckListener>();

        using var sub = ctx.DeviceStateReports.Subscribe(report =>
        {
            if (report.NewDevice)
            {
                Console.WriteLine($"NEW DEV: {report.DeviceReference.DeviceName} - {report.Connected}");
            }
            else
            {
                Console.WriteLine($"CHANGED: {report.DeviceReference.DeviceName} - {report.Connected}");
            }
        });

        Console.WriteLine("Current Devices:");

        foreach (var d in ctx.KnownDevices)
        {
            Console.WriteLine($" - INIT: {d.DeviceName} - {d.Connected}");
        }

        PrintInfoBlock();
        Console.ReadKey();
    }

    private static void RedrawDeviceList(IDeviceContext context)
    {
        // Alternative to Console.Clear without flicker.
        Console.SetCursorPosition(0, 0);

        PrintInfoBlock();
        Console.WriteLine("Devices:");

        if (context.KnownDevices.Count == 0)
        {
            Console.WriteLine("   (none)");
        }
        else
        {
            foreach (var d in context.KnownDevices)
            {
                Console.WriteLine($" - [{(d.Connected ? 'X' : ' ')}] {d.DeviceName}");
            }
        }
    }

    private static void PrintInfoBlock()
    {
        Console.WriteLine();
        Console.WriteLine("Try to connect and disconnect some of your devices.");
        Console.WriteLine("Press any key (on PC keyboard) to exit this example.");
        Console.WriteLine();
        Console.WriteLine();
    }
}
