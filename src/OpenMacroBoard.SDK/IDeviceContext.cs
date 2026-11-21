using System;
using System.Collections.Generic;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Represents a context that tracks devices and their connection state using device listeners.
/// </summary>
/// <remarks>
/// <para>This is basically the entry point to opening a <see cref="IMacroBoard"/>. Once constructed
/// you can add device listeners for various providers and the context will collect all devices
/// (even from different providers) in one place.</para>
/// </remarks>
public interface IDeviceContext : IDisposable
{
    /// <summary>
    /// Gets the observable that reports all device state changes, for example like
    /// new devices or connection state changes.
    /// </summary>
    /// <remarks>
    /// <para>Once subscribed, reports are generated to reflect the current state of known devices and
    /// their connection state. This does not mean that all original reports are replayed, but only
    /// the most recent events, that are needed to rebuild the currently known state for all known devices.</para>
    /// </remarks>
    IObservable<DeviceStateReport> DeviceStateReports { get; }

    /// <summary>
    /// A list of known devices.
    /// </summary>
    /// <remarks>
    /// <para>The order of the list is consistent and new devices are always added at the end.</para>
    /// </remarks>
    IReadOnlyList<IKnownDevice> KnownDevices { get; }

    /// <summary>
    /// Registers a new device listener for that context.
    /// </summary>
    /// <remarks>
    /// <para>Registered listeners can't be unsubscribed individually.
    /// All listeners will unsubscribed when the context is disposed.</para>
    /// </remarks>
    /// <returns>Returns the original instance to allow for fluent API calls.</returns>
    IDeviceContext AddListener(IObservable<DeviceStateReport> deviceListener);

    /// <summary>
    /// Registers a new device listener for that context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registered listeners can't be unsubscribed individually.
    /// All listeners will unsubscribed when the context is disposed.
    /// </para>
    /// <para>
    /// When <paramref name="disposeWithContext"/> is set to true and <paramref name="deviceListener"/> is
    /// <see cref="IDisposable"/> it will be disposed when the context is disposed.
    /// </para>
    /// </remarks>
    /// <returns>Returns the original instance to allow for fluent API calls.</returns>
    IDeviceContext AddListener(IObservable<DeviceStateReport> deviceListener, bool disposeWithContext);

    /// <summary>
    /// Registers a new device listener for that context.
    /// </summary>
    /// <typeparam name="TListener">A <see cref="IObservable{DeviceStateReport}"/> with a parameterless constructor.</typeparam>
    /// <remarks>
    /// <para>Registered listeners can't be unsubscribed individually.
    /// All listeners will unsubscribed when the context is disposed.</para>
    /// </remarks>
    /// <returns>Returns the original instance to allow for fluent API calls.</returns>
    IDeviceContext AddListener<TListener>()
        where TListener : IObservable<DeviceStateReport>, new();
}
