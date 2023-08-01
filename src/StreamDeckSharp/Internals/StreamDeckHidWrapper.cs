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
        private readonly object hidStreamLock = new();
        private readonly string devicePath;

        /// <summary>
        /// Used to throttle write speed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Based on a hand full of speed measurements, it looks like that (at least)
        /// the classical stream deck (hardware revision 1) can't keep up with full USB 2.0 speed.
        /// </para>
        /// <para>
        /// For the other devices this limit is also active but probably not relevant,
        /// because in practice the speed is slower, because all other devices use
        /// JPEG instead of BMP and the Hid.Write probably also blocks as long as the device is busy.
        /// </para>
        /// <para>
        /// The limit was determined by the following measurements with a classical stream deck:</para>
        /// <para>
        /// write speed -> time between glitches<br/>
        /// 3.90 MiB/s -> 1.7s<br/>
        /// 3.68 MiB/s -> 3.7s<br/>
        /// 3.60 MiB/s -> 7.6s<br/>
        /// </para>
        /// <para>
        /// Based on the assumption, that the stream deck has a maximum speed at which data is processed,
        /// the following formular can be used:
        /// </para>
        /// <para>
        /// Measured speed ............ s<br/>
        /// Time between glitches ..... t<br/>
        /// Internal speed ............ x (to be calculated)<br/>
        /// Hardware buffer size ...... b (will be eliminated when solving for x)<br/>
        /// </para>
        /// <para>(s - x) * t = b</para>
        /// <para>(s1 - x) * t1 = (s2 - x) * t2</para>
        /// <para>
        /// When solved for x and evaluated with all the measured pairs, the calculated internal speed
        /// of the classical stream deck seems to be (almost exactly?) 3.50 MiB/s - A few tests indeed
        /// showed that limiting the speed below that value seems to prevent glitches.
        /// </para>
        /// <para>
        /// So long story short we set a limit of 3'200'000 bytes/s (~3.0 MiB/s)
        /// for all devices that can't keep up or I haven't had the chance to test on a
        /// particular Elgato Device (for example I don't own a StreamDeck Rev2) and for
        /// other devices that work as expected we set <see cref="double.PositiveInfinity"/> (unlimited).
        /// </para>
        /// </remarks>
        private readonly Throttle throttle;

        private readonly IStreamDeckHidComDriver hardwareInfo;
        private HidStream dStream;
        private byte[] readReportBuffer;

        public StreamDeckHidWrapper(HidDevice device, IStreamDeckHidComDriver hardwareInfo)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            this.hardwareInfo = hardwareInfo ?? throw new ArgumentNullException(nameof(hardwareInfo));

            if (hardwareInfo.BytesPerSecondLimit < double.PositiveInfinity)
            {
                throttle = new() { BytesPerSecondLimit = hardwareInfo.BytesPerSecondLimit };
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
                    throttle?.MeasureAndBlock(data.Length);
                    targetStream.GetFeature(data);
                    return true;
                }
            }
            catch (Exception ex) when (ex is TimeoutException or IOException)
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
                    throttle?.MeasureAndBlock(featureData.Length);
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
                    throttle?.MeasureAndBlock(reportData.Length);
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
            var inputReportLength = device.GetMaxInputReportLength();
            OutputReportLength = device.GetMaxOutputReportLength();
            FeatureReportLength = device.GetMaxFeatureReportLength();

            Debug.Assert(
                OutputReportLength == hardwareInfo.ExpectedOutputReportLength,
                $"Output report length unexpected. Found: {OutputReportLength}. Expected: {hardwareInfo.ExpectedOutputReportLength}"
            );

            Debug.Assert(
                FeatureReportLength == hardwareInfo.ExpectedFeatureReportLength,
                $"Feature report length unexpected. Found: {FeatureReportLength}. Expected: {hardwareInfo.ExpectedFeatureReportLength}"
            );

            Debug.Assert(
                inputReportLength == hardwareInfo.ExpectedInputReportLength,
                $"Input report length unexpected. Found: {inputReportLength}. Expected: {hardwareInfo.ExpectedInputReportLength}"
            );

            readReportBuffer = new byte[OutputReportLength];
        }

        private void RefreshConnection()
        {
            var device = DeviceList.Local
                .GetHidDevices()
                .FirstOrDefault(d => d.DevicePath == devicePath);

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
            var dStreamRefCopy = dStream;
            dStream = null;

            if (dStreamRefCopy is null)
            {
                return;
            }

            dStreamRefCopy.Dispose();
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
                if (dStream == null)
                {
                    // connection already disposed
                    return;
                }

                var res = stream.EndRead(ar);
                var data = new byte[res];
                Array.Copy(readReportBuffer, 0, data, 0, res);
                ReportReceived?.Invoke(this, new ReportReceivedEventArgs(data));
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                DisposeConnection();
                return;
            }

            BeginWaitRead(stream);
        }
    }
}
