using HidSharp;
using OpenMacroBoard.SDK;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckHidWrapper : IStreamDeckHid
    {
        private readonly string devicePath;

        private HidStream dStream;
        private byte[] readReportBuffer;

        public StreamDeckHidWrapper(HidDevice device)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            devicePath = device.DevicePath;
            DeviceList.Local.Changed += Local_Changed;

            InitializeDeviceSettings(device);
            OpenConnection(device);
        }

        private void Local_Changed(object sender, DeviceListChangedEventArgs e)
        {
            RefreshConnection();
        }

        private void InitializeDeviceSettings(HidDevice device)
        {
            if (readReportBuffer is null)
            {
                OutputReportLength = device.GetMaxOutputReportLength();
                FeatureReportLength = device.GetMaxFeatureReportLength();
                readReportBuffer = new byte[OutputReportLength];
            }
        }

        private void RefreshConnection()
        {
            var device = DeviceList
                            .Local
                            .GetHidDevices()
                            .Where(d => d.DevicePath == devicePath)
                            .FirstOrDefault();

            var deviceFound = device != null;
            var deviceActive = dStream != null;

            if (deviceFound == deviceActive)
            {
                return;
            }

            if (!deviceFound)
            {
                DisposeConnection();
            }
            else
            {
                OpenConnection(device);
            }
        }

        private void OpenConnection(HidDevice device)
        {
            if (device == null)
            {
                return;
            }

            if (dStream != null)
            {
                return;
            }

            if (device.TryOpen(out HidStream stream))
            {
                stream.ReadTimeout = Timeout.Infinite;
                dStream = stream;
                BeginWaitRead(stream);
                ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(true));
            }
        }

        private void DisposeConnection()
        {
            if (dStream is null)
            {
                return;
            }

            dStream.Dispose();
            dStream = null;
            ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(false));
        }

        public int OutputReportLength { get; private set; }
        public int FeatureReportLength { get; private set; }

        public bool IsConnected => dStream != null;

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<ReportReceivedEventArgs> ReportReceived;

        public void Dispose()
        {
            DisposeConnection();
        }

        public bool ReadFeatureData(byte id, out byte[] data)
        {
            data = new byte[FeatureReportLength];
            data[0] = id;

            var targetStream = dStream;

            if (targetStream is null)
            {
                return false;
            }

            try
            {
                dStream.GetFeature(data);
                return true;
            }
            catch (Exception ex) when (ex is TimeoutException || ex is IOException)
            {
                DisposeConnection();
                return false;
            }
        }

        public bool WriteFeature(byte[] featureData)
        {
            if (featureData.Length != FeatureReportLength)
            {
                var resizedData = new byte[FeatureReportLength];
                var minLen = Math.Min(FeatureReportLength, featureData.Length);
                Array.Copy(featureData, 0, resizedData, 0, minLen);
                featureData = resizedData;
            }

            var targetStream = dStream;

            if (targetStream is null)
            {
                return false;
            }

            try
            {
                dStream.SetFeature(featureData);
                return true;
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                DisposeConnection();
                return false;
            }
        }

        public bool WriteReport(byte[] reportData)
        {
            var targetStream = dStream;

            if (targetStream is null)
            {
                return false;
            }

            try
            {
                targetStream.Write(reportData);
                return true;
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                DisposeConnection();
                return false;
            }
        }

        private void BeginWaitRead(HidStream stream)
        {
            stream.BeginRead(readReportBuffer, 0, readReportBuffer.Length, new AsyncCallback(ReadReportCallback), stream);
        }

        private void ReadReportCallback(IAsyncResult ar)
        {
            var stream = (HidStream)ar.AsyncState;

            try
            {
                var res = stream.EndRead(ar);
                var data = new byte[res];
                Array.Copy(readReportBuffer, 0, data, 0, res);
                ReportReceived?.Invoke(this, new ReportReceivedEventArgs(data));
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                Debug.WriteLine($"EXCEPTION: ({ex.GetType()}) {ex.Message}");
                DisposeConnection();
                return;
            }

            BeginWaitRead(stream);
        }

        private static bool IsConnectionError(Exception ex)
        {
            if (ex is TimeoutException)
            {
                return true;
            }

            if (ex is IOException)
            {
                return true;
            }

            if (ex is ObjectDisposedException)
            {
                return true;
            }

            return false;
        }
    }
}
