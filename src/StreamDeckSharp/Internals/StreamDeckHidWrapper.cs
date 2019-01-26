using HidLibrary;
using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckHidWrapper : IStreamDeckHid
    {
        private HidDevice device;
        private readonly ConnectionEventArgs connected = new ConnectionEventArgs(true);
        private readonly ConnectionEventArgs disconnected = new ConnectionEventArgs(false);

        public StreamDeckHidWrapper(HidDevice device)
        {
            if (device == null)
                throw new ArgumentNullException();

            if (device.IsOpen)
                throw new NotSupportedException();

            device.MonitorDeviceEvents = true;
            device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            if (!device.IsOpen)
                throw new Exception("Device could not be opened");

            this.device = device;
            this.device.Inserted += () => ConnectionStateChanged?.Invoke(this, connected);
            this.device.Removed += () => ConnectionStateChanged?.Invoke(this, disconnected);
        }

        public int OutputReportLength
            => device.Capabilities.OutputReportByteLength;

        public bool IsConnected
            => device.IsConnected;

        public byte[] ReadReport()
            => device.ReadReport().Data;

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        public void Dispose()
        {
            device.SetAllEvents(true);
            device.CloseDevice();
            device.Dispose();
            device = null;
        }

        public bool WriteFeature(byte[] featureData)
            => device?.WriteFeatureData(featureData) ?? false;

        public bool WriteReport(byte[] reportData)
            => device?.Write(reportData) ?? false;

        public byte[] ReadFeatureData(byte id)
        {
            device.ReadFeatureData(out var data, id);
            return data;
        }
    }
}
