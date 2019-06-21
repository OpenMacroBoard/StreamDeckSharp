using HidLibrary;
using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckHidWrapper : IStreamDeckHid
    {
        private HidDevice device;
        private bool disposed = false;

        private readonly object disposeLock = new object();
        private readonly object readReportLock = new object();
        private readonly object readFeatureLock = new object();
        private readonly object writeReportLock = new object();
        private readonly object writeFeatureLock = new object();

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
        {
            lock (readReportLock)
            {
                VerifyDisposeWasNotCalled();
                return device.ReadReport().Data;
            }
        }

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        public bool WriteFeature(byte[] featureData)
        {
            lock (writeFeatureLock)
            {
                VerifyDisposeWasNotCalled();
                return device.WriteFeatureData(featureData);
            }
        }

        public bool WriteReport(byte[] reportData)
        {
            lock (writeReportLock)
            {
                VerifyDisposeWasNotCalled();
                return device.Write(reportData);
            }
        }

        public byte[] ReadFeatureData(byte id)
        {
            lock (readFeatureLock)
            {
                VerifyDisposeWasNotCalled();
                device.ReadFeatureData(out var data, id);
                return data;
            }
        }

        public void Dispose()
        {
            lock (disposeLock)
            {
                if (disposed)
                    return;
                disposed = true;
            }

            device.SetAllEvents(true);

            lock (writeReportLock)
                lock (writeFeatureLock)
                    lock (readReportLock)
                        lock (readFeatureLock)
                        {
                            device.CloseDevice();
                            device.Dispose();
                            device = null;
                        }
        }

        private void VerifyDisposeWasNotCalled()
        {
            lock (disposeLock)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(StreamDeckHidWrapper));
            }
        }
    }
}
