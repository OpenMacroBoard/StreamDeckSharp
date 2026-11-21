using HidSharp;
using OpenMacroBoard.SDK;
using StreamDeckSharp.Exceptions;
using StreamDeckSharp.Internals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp;

/// <summary>
/// This is a factory class to create IStreamDeck References
/// </summary>
public static class StreamDeck
{
    /// <summary>
    /// Enumerates connected Stream Decks and returns the first one.
    /// </summary>
    /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
    public static IMacroBoard OpenDevice(params IUsbHidHardware[] hardware)
    {
        return OpenDevice(StreamDeckOptions.Default, hardware);
    }

    /// <summary>
    /// Enumerates connected Stream Decks and returns the first one.
    /// </summary>
    /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
    public static IMacroBoard OpenDevice(bool useWriteCache, params IUsbHidHardware[] hardware)
    {
        return OpenDevice(StreamDeckOptions.Default with { UseWriteCache = useWriteCache }, hardware);
    }

    /// <summary>
    /// Enumerates connected Stream Decks and returns the first one.
    /// </summary>
    /// <param name="options">Options for the device, for example the JPEG encoder to use.</param>
    /// <param name="hardware">If no types or null is passed as argument, all known types are found</param>
    /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
    public static IMacroBoard OpenDevice(StreamDeckOptions options, params IUsbHidHardware[] hardware)
    {
        var dev = EnumerateDevices(options, hardware).FirstOrDefault() ?? throw new StreamDeckNotFoundException();
        return dev.Open();
    }

    /// <summary>
    /// Enumerates connected Stream Decks and returns the first one.
    /// </summary>
    /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
    public static IMacroBoard OpenDevice(string devicePath)
    {
        return OpenDevice(devicePath, StreamDeckOptions.Default);
    }

    /// <summary>
    /// Get the Stream Deck with a given <paramref name="devicePath"/>.
    /// </summary>
    /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
    public static IMacroBoard OpenDevice(string devicePath, bool useWriteCache)
    {
        return OpenDevice(devicePath, StreamDeckOptions.Default with { UseWriteCache = useWriteCache });
    }

    /// <summary>
    /// Get the Stream Deck with a given <paramref name="devicePath"/>.
    /// </summary>
    /// <param name="devicePath">The OSes unique identifier of the device.</param>
    /// <param name="options">Options for the device, for example the JPEG encoder to use.</param>
    /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
    public static IMacroBoard OpenDevice(string devicePath, StreamDeckOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dev = DeviceList.Local.GetHidDevices().First(d => d.DevicePath == devicePath);
        return FromHid(dev ?? throw new StreamDeckNotFoundException(), options);
    }

    /// <summary>
    /// Enumerate Elgato Stream Deck Devices that match a given type.
    /// </summary>
    /// <param name="hardware">If no types or null is passed as argument, all known types are found</param>
    public static IEnumerable<StreamDeckDeviceReference> EnumerateDevices(params IUsbHidHardware[] hardware)
    {
        return EnumerateDevices(StreamDeckOptions.Default, hardware);
    }

    /// <summary>
    /// Enumerate Elgato Stream Deck Devices that match a given type.
    /// </summary>
    /// <param name="options">Options that are used when one of the devices is opened.</param>
    /// <param name="hardware">If no types or null is passed as argument, all known types are found</param>
    public static IEnumerable<StreamDeckDeviceReference> EnumerateDevices(
        StreamDeckOptions options,
        params IUsbHidHardware[] hardware
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        return DeviceList.Local.GetStreamDecks(options, hardware);
    }

    internal static IMacroBoard FromHid(HidDevice device, StreamDeckOptions options)
    {
        var hwInfo = device.GetHardwareInformation();
        var hidWrapper = new StreamDeckHidWrapper(device, hwInfo.Driver);

        if (options.UseWriteCache)
        {
            return new CachedHidClient(hidWrapper, hwInfo.Keys, hwInfo.Driver, options.JpegEncoder);
        }

        return new BasicHidClient(hidWrapper, hwInfo.Keys, hwInfo.Driver, options.JpegEncoder);
    }
}
