using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO;
using StreamDeckSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace OpenMacroBoard.Examples.CommonStuff;

public static class ExampleHelper
{
    private static int ctrlCCnt = 0;

    static ExampleHelper()
    {
        ImageSharpWarmupHelper.RunWarmup();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Interlocked.Increment(ref ctrlCCnt);
        };
    }

    public static int CtrlCCount => ctrlCCnt;
    public static bool CtrlCWasPressed => ctrlCCnt > 0;

    public static IDeviceReference SelectBoard(IEnumerable<IDeviceReference> devices)
    {
        var devList = devices.ToList();
        devList.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));

        if (devList.Count < 1)
        {
            return null;
        }

        if (devList.Count == 1)
        {
            return devList[0];
        }

        var selected = ConsoleSelect(devList);
        Console.Clear();

        return selected;
    }

    public static T ConsoleSelect<T>(IEnumerable<T> elements)
    {
        var list = elements.ToArray();
        var select = -1;

        for (var i = 0; i < list.Length; i++)
        {
            Console.WriteLine($"[{i}] {list[i]}");
        }

        Console.WriteLine();

        do
        {
            Console.Write("Select: ");
            var selection = Console.ReadLine();

            if (int.TryParse(selection, out var id))
            {
                select = id;
            }
        }
        while (select < 0 || select >= list.Length);

        return list[select];
    }

    public static void WaitForKeyToExit()
    {
        Console.WriteLine("Please press any key (on PC keyboard) to exit this example.");
        Console.ReadKey();
    }

    public static IMacroBoard OpenBoard(Predicate<IDeviceReference> boardSelector)
    {
        using var ctx = DeviceContext.Create()
            .AddListener<StreamDeckListener>()
            .AddListener<SocketIOBoardListener>()
            ;

        var sync = new object();
        IReadOnlyList<IKnownDevice> deviceList = [];

        void UpdateAndRedraw()
        {
            lock (sync)
            {
                deviceList = ctx.KnownDevices.Where(d => boardSelector(d)).ToList();
                RedrawDeviceList(deviceList);
            }
        }

        using var subscription = ctx.DeviceStateReports.Subscribe(_ => UpdateAndRedraw());

        while (true)
        {
            UpdateAndRedraw();
            var selection = Console.ReadLine();

            var refcopyDeviceList = deviceList;

            if (!int.TryParse(selection, out var id))
            {
                continue;
            }

            if (id >= 0 && id < refcopyDeviceList.Count)
            {
                Console.Clear();

                var device = refcopyDeviceList[id]
                    .Open()
                    .WithDisconnectReplay()
                    .WithButtonPressEffect();

                device.SetBrightness(100);
                return device;
            }
        }
    }

    public static IMacroBoard OpenBoard()
    {
        return OpenBoard(x => true);
    }

    private static void RedrawDeviceList(IReadOnlyList<IKnownDevice> devices)
    {
        // Alternative to Console.Clear without flicker.
        Console.SetCursorPosition(0, 0);

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Devices:");

        if (devices.Count == 0)
        {
            Console.WriteLine("   (none)");
        }
        else
        {
            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                Console.WriteLine($"{i,3}:  [{(d.Connected ? 'X' : ' ')}] {d.DeviceName}");
            }
        }

        Console.SetCursorPosition(0, 0);

        var text = "Select a device: ";
        Console.Write(text.PadRight(Console.BufferWidth - 1));
        Console.SetCursorPosition(0, 0);
        Console.Write(text);
    }
}
