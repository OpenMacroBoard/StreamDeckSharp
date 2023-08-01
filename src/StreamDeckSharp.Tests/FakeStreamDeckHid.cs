using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals;
using System;
using System.Collections.Generic;
using System.IO;

namespace StreamDeckSharp.Tests
{
    internal sealed class FakeStreamDeckHid : IStreamDeckHid
    {
        private readonly TextWriter log;
        private readonly UsbHardwareIdAndDriver hardware;

        public FakeStreamDeckHid(TextWriter log, UsbHardwareIdAndDriver hardware)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        }

        public event EventHandler<ReportReceivedEventArgs> ReportReceived;
        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        public bool IsConnected => throw new NotImplementedException();
        public int OutputReportLength => hardware.Driver.ExpectedOutputReportLength;

        public int BytesPerLineOutput { get; set; } = 16;

        /// <summary>
        /// The response this fake stream deck HID uses to respond
        /// to <see cref="WriteFeature(byte[])"/> calls.
        /// </summary>
        public bool WriteFeatureResponse { get; set; } = true;

        /// <summary>
        /// The response this fake stream deck HID uses to respond
        /// to <see cref="WriteReport(byte[])"/> calls.
        /// </summary>
        public bool WriteReportResponse { get; set; } = true;

        /// <summary>
        /// A queue of answers this fake stream deck HID uses to respond
        /// to <see cref="ReadFeatureData(byte, out byte[])"/> calls.
        /// </summary>
        public Queue<(byte Id, bool ReturnValue, byte[] FeatureData)> ReadFeatureResonseQueue { get; } = new();

        public void Dispose()
        {
            log.WriteLine("Dispose()");
        }

        public bool ReadFeatureData(byte id, out byte[] data)
        {
            var (expextedId, returnValue, featureData) = ReadFeatureResonseQueue.Dequeue();

            if (expextedId != id)
            {
                throw new InvalidOperationException("Given id didn't match expectation.");
            }

            data = featureData;
            return returnValue;
        }

        public bool WriteFeature(byte[] featureData)
        {
            return log.WriteBinaryBlock("Feature", WriteFeatureResponse, featureData, BytesPerLineOutput);
        }

        public bool WriteReport(byte[] reportData)
        {
            return log.WriteBinaryBlock("Report", WriteReportResponse, reportData, BytesPerLineOutput);
        }

        public void FakeIncommingInputReport(byte[] data)
        {
            ReportReceived?.Invoke(this, new ReportReceivedEventArgs(data));
        }

        public void FakeConnectionStateChange(ConnectionEventArgs args)
        {
            ConnectionStateChanged?.Invoke(this, args);
        }
    }
}
