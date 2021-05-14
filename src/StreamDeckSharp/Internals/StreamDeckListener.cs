using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckListener : IStreamDeckListener
    {
        private readonly object sync = new object();
        private readonly IUsbHidHardware[] hardwareFilter;

        private readonly Dictionary<string, DeviceReferenceHandle> knownDeviceLookup;
        private readonly List<IStreamDeckRefHandle> knownStreamDecks;

        private Dictionary<string, DeviceReferenceHandle> lastScan;
        private bool disposed = true;

        public StreamDeckListener(params IUsbHidHardware[] hardwareFilter)
        {
            lastScan = new Dictionary<string, DeviceReferenceHandle>();

            this.hardwareFilter = hardwareFilter;

            knownDeviceLookup = new Dictionary<string, DeviceReferenceHandle>();
            knownStreamDecks = new List<IStreamDeckRefHandle>();
            KnownStreamDecks = knownStreamDecks.AsReadOnly();

            // register event handler before we load the entire list
            // so we don't miss stream decks connecting between the calls.
            DeviceList.Local.Changed += DeviceListChanged;

            // initial force load
            ProcessDelta();
        }

        public event EventHandler<StreamDeckConnectionChangedEventArgs> NewDeviceConnected;
        public event EventHandler<StreamDeckConnectionChangedEventArgs> ConnectionChanged;

        public IReadOnlyList<IStreamDeckRefHandle> KnownStreamDecks { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            DeviceList.Local.Changed -= DeviceListChanged;
        }

        private void ProcessDelta()
        {
            lock (sync)
            {
                // because the HidDevice event doesn't tell us what changed
                // we calculate the difference ourselves.
                var currentDevices = DeviceList.Local
                    .GetStreamDecks(hardwareFilter)
                    .ToDictionary(s => s.DevicePath, s => s);

                var connectedDevices = currentDevices
                    .Select(d => d.Key)
                    .Where(d => !lastScan.ContainsKey(d))
                    .ToList();

                var disconnectedDevices = lastScan
                    .Select(d => d.Key)
                    .Where(d => !currentDevices.ContainsKey(d))
                    .ToList();

                foreach (var devicePath in connectedDevices)
                {
                    var isNew = !knownDeviceLookup.ContainsKey(devicePath);

                    // reuse known device reference if there is one
                    // a user of the library might compare references or use the
                    // reference as a key in a dictonary or set.
                    var deviceRef = isNew ? currentDevices[devicePath] : knownDeviceLookup[devicePath];

                    var eventArg = new StreamDeckConnectionChangedEventArgs(deviceRef, true);

                    // Add first to make sure a consumer
                    // of the event has an up to date list
                    if (isNew)
                    {
                        knownDeviceLookup.Add(devicePath, deviceRef);
                        knownStreamDecks.Add(deviceRef);

                        NewDeviceConnected?.Invoke(this, eventArg);
                    }

                    ConnectionChanged?.Invoke(this, eventArg);
                }

                foreach (var devicePath in disconnectedDevices)
                {
                    var eventArg = new StreamDeckConnectionChangedEventArgs(knownDeviceLookup[devicePath], false);
                    ConnectionChanged?.Invoke(this, eventArg);
                }

                lastScan = currentDevices;
            }
        }

        private void DeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            ProcessDelta();
        }
    }
}
