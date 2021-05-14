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
        private readonly object hidStreamLock = new object();
        private readonly string devicePath;

        private readonly Throttle throttle = new Throttle()
        {
            // Based on a hand full of speed measurements, it looks like that (at least)
            // the classical stream deck (hardware revision 1) can't keep up with full USB 2.0 speed.
            //
            // For the other devices this limit is also active but probably not relevant,
            // because in practice the speed is slower, because all other devices use
            // JPEG instead of BMP and the Hid.Write probably also blocks as long as the device is busy.
            //
            // The limit was determined by the following measurements with a classical stream deck:
            //
            // write speed -> time between glitches
            // 3.90 MiB/s -> 1.7s
            // 3.68 MiB/s -> 3.7s
            // 3.60 MiB/s -> 7.6s
            //
            // Based on the assumption, that the stream deck has a maximum speed at which data is processed,
            // the following formular can be used:
            //
            // Measured speed ............ s
            // Time between glitches ..... t
            // Internal speed ............ x (to be calculated)
            // Hardware buffer size ...... b (will be eliminated when solving for x)
            //
            // (s - x) * t = b
            //
            // (s1 - x) * t1 = (s2 - x) * t2
            //
            // When solved for x and evaluated with all the measured pairs, the calculated internal speed
            // of the classical stream deck seems to be (almost exactly?) 3.50 MiB/s - A few tests indeed
            // showed that limiting the speed below that value seems to prevent glitches.
            //
            // So long story short we set a limit of 3'200'000 bytes/s (~3.0 MiB/s)
            BytesPerSecondLimit = 3_200_000,
        };

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

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<ReportReceivedEventArgs> ReportReceived;

        public int OutputReportLength { get; private set; }
        public int FeatureReportLength { get; private set; }

        public bool IsConnected => dStream != null;

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
                lock (hidStreamLock)
                {
                    throttle.MeasureAndBlock(data.Length);
                    targetStream.GetFeature(data);
                    return true;
                }
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
                lock (hidStreamLock)
                {
                    throttle.MeasureAndBlock(featureData.Length);
                    targetStream.SetFeature(featureData);
                }

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
                lock (hidStreamLock)
                {
                    throttle.MeasureAndBlock(reportData.Length);
                    targetStream.Write(reportData);
                }

                return true;
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                DisposeConnection();
                return false;
            }
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

            if (device.TryOpen(out var stream))
            {
                stream.ReadTimeout = Timeout.Infinite;
                dStream = stream;
                BeginWaitRead(stream);
                ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(true));
            }
        }

        private void Local_Changed(object sender, DeviceListChangedEventArgs e)
        {
            RefreshConnection();
        }

        private void InitializeDeviceSettings(HidDevice device)
        {
            OutputReportLength = device.GetMaxOutputReportLength();
            FeatureReportLength = device.GetMaxFeatureReportLength();
            readReportBuffer = new byte[OutputReportLength];
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
    }
}
