#region License
/* Copyright 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
#endregion

using System;
using System.Threading;

namespace HidSharp.Reports.Input
{
    public class HidDeviceInputReceiver
    {
        public event EventHandler Started;
        public event EventHandler Received;
        public event EventHandler Stopped;

        byte[] _buffer; int _bufferOffset, _bufferCount;
        int _maxInputReportLength;
        ReportDescriptor _reportDescriptor;
        volatile bool _running;
        HidStream _stream;
        object _syncRoot;
        ManualResetEvent _waitHandle;

        public HidDeviceInputReceiver(ReportDescriptor reportDescriptor)
        {
            Throw.If.Null(reportDescriptor, "reportDescriptor");
            _maxInputReportLength = reportDescriptor.MaxInputReportLength;
            _buffer = new byte[_maxInputReportLength * 16];
            _reportDescriptor = reportDescriptor;
            _syncRoot = new object();
            _waitHandle = new ManualResetEvent(true);
        }

        // TODO: Handle the case where the library user is not calling TryRead fast enough for the HID device.
        /// <summary>
        /// Starts the receiver. It will continue until the stream is closed or the device is disconnected.
        /// Be sure to call <see cref="TryRead"/> to read HID reports as they come in.
        /// </summary>
        /// <param name="stream">The stream to receive on.</param>
        public void Start(HidStream stream)
        {
            Throw.If.Null(stream);

            lock (_syncRoot)
            {
                int length = _maxInputReportLength;
                if (length == 0) { return; } // Nothing to parse.

                stream.ReadTimeout = Timeout.Infinite;
                if (_running) { throw new InvalidOperationException("The receiver is already running."); }
                _running = true; _stream = stream;

                byte[] buffer = new byte[length * 16];
                Action beginRead = null; AsyncCallback endRead = null;
                beginRead = () =>
                {
                    try { stream.BeginRead(buffer, 0, buffer.Length, endRead, null); }
                    catch { Stop(); return; }
                };
                endRead = ar =>
                {
                    int count;
                    try { count = stream.EndRead(ar); }
                    catch { Stop(); return; }

                    if (count == 0) { Stop(); return; }

                    ProvideReceivedData(buffer, 0, count);
                    beginRead();
                };
                beginRead();

                _waitHandle.Reset();
            }

            var ev = Started;
            if (ev != null) { ev(this, EventArgs.Empty); }
        }

        void Stop()
        {
            lock (_syncRoot)
            {
                _running = false;
                _stream = null;
                _waitHandle.Set();
            }

            var ev = Stopped;
            if (ev != null) { ev(this, EventArgs.Empty); }
        }

        void ClearReceivedData()
        {
            lock (_syncRoot)
            {
                _bufferOffset = 0;
                _bufferCount = 0;
                _waitHandle.Reset();
            }
        }

        void ProvideReceivedData(byte[] buffer, int offset, int count)
        {
            Throw.If.Null(buffer, "buffer").OutOfRange(buffer, offset, count);

            lock (_syncRoot)
            {
                if (_maxInputReportLength == 0) { return; } // Nothing to parse.

                int neededLength = checked(_bufferCount + count);
                int bufferLength = _buffer.Length;
                while (bufferLength < neededLength) { bufferLength = checked(bufferLength * 2); }
                Array.Resize(ref _buffer, bufferLength);

                Array.Copy(buffer, 0, _buffer, _bufferCount, count);
                _bufferCount += count;
                _waitHandle.Set();
            }

            var ev = Received;
            if (ev != null) { ev(this, EventArgs.Empty); }
        }

        /// <summary>
        /// Checks for pending HID reports.
        /// </summary>
        /// <param name="buffer">The buffer to write the report to.</param>
        /// <param name="offset">The offset to begin writing the report at.</param>
        /// <param name="report">The <see cref="HidSharp.Reports.Report"/> the buffer conforms to.</param>
        /// <returns><c>true</c> if there was a pending report.</returns>
        public bool TryRead(byte[] buffer, int offset, out Report report)
        {
            Throw.If.Null(buffer).OutOfRange(buffer, offset, _maxInputReportLength);

            lock (_syncRoot)
            {
                if (!_running)
                {
                    report = null; return false;
                }

                if (_bufferOffset >= _bufferCount)
                {
                    _waitHandle.Reset();
                    report = null; return false;
                }

                if (!_reportDescriptor.TryGetReport(ReportType.Input, _buffer[_bufferOffset], out report))
                {
                    // Unknown report!
                    ClearReceivedData();
                    report = null; return false;
                }

                int reportLength = report.Length;
                int finalBufferOffset = _bufferOffset + reportLength;
                if (finalBufferOffset > _bufferCount)
                {
                    // Not completely received!
                    _waitHandle.Reset();
                    report = null; return false;
                }

                Array.Copy(_buffer, _bufferOffset, buffer, offset, reportLength);
                if (finalBufferOffset == _bufferCount)
                {
                    // Nothing more to read!
                    ClearReceivedData();
                }
                else
                {
                    // There is more to read.
                    _bufferOffset = finalBufferOffset;
                }

                return true;
            }
        }

        /// <summary>
        /// <c>true</c> if the receiver is running.
        /// <c>false</c> if the receiver has stopped, or has not yet been started.
        /// </summary>
        public bool IsRunning
        {
            get { return _running; }
        }

        /// <summary>
        /// The <see cref="HidSharp.Reports.ReportDescriptor"> used to separate out reports.
        /// </summary>
        public ReportDescriptor ReportDescriptor
        {
            get { return _reportDescriptor; }
        }

        /// <summary>
        /// The stream associated with this receiver.
        /// 
        /// </summary>
        public HidStream Stream
        {
            get { return _stream; }
        }

        /// <summary>
        /// This will be signaled any time there is data, or when the receiver has stopped due to stream closure or device disconnect.
        /// To clear the signal, call <see cref="TryRead"/> until there is no more data.
        /// If the receiver has stopped, the signal cannot be cleared.
        /// </summary>
        public WaitHandle WaitHandle
        {
            get { return _waitHandle; }
        }
    }
}
