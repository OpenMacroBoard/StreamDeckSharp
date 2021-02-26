#region License
/* Copyright 2017-2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    sealed class WinSerialStream : SysSerialStream
    {
        object _lock = new object();
        IntPtr _handle, _closeEventHandle;

        SerialSettings _ser;
        bool _settingsChanged;

        internal WinSerialStream(WinSerialDevice device)
            : base(device)
        {
            _closeEventHandle = NativeMethods.CreateManualResetEventOrThrow();
        }

        internal void Init(string devicePath)
        {
            IntPtr handle = NativeMethods.CreateFileFromDevice(devicePath,
                                                               NativeMethods.EFileAccess.Read | NativeMethods.EFileAccess.Write,
                                                               NativeMethods.EFileShare.None);
            if (handle == (IntPtr)(-1))
            {
                int hr = Marshal.GetHRForLastWin32Error();
                throw DeviceException.CreateIOException(Device, "Unable to open serial device (" + devicePath + ").", hr);
            }

            var timeouts = new NativeMethods.COMMTIMEOUTS();
            timeouts.ReadIntervalTimeout = uint.MaxValue;
            timeouts.ReadTotalTimeoutConstant = uint.MaxValue - 1; // CP210x fails if this is set to uint.MaxValue.
            timeouts.ReadTotalTimeoutMultiplier = uint.MaxValue;
            if (!NativeMethods.SetCommTimeouts(handle, out timeouts))
            {
                int hr = Marshal.GetHRForLastWin32Error();
                NativeMethods.CloseHandle(handle);
                throw DeviceException.CreateIOException(Device, "Unable to set serial timeouts.", hr);
            }

            _handle = handle;
            HandleInitAndOpen();
        }

        ~WinSerialStream()
        {
            Close();
            NativeMethods.CloseHandle(_closeEventHandle);
        }

        protected override void Dispose(bool disposing)
        {
            if (!HandleClose()) { return; }

            NativeMethods.SetEvent(_closeEventHandle);
            HandleRelease();

            base.Dispose(disposing);
        }

        internal override void HandleFree()
        {
            NativeMethods.CloseHandle(ref _handle);
            NativeMethods.CloseHandle(ref _closeEventHandle);
        }

        public override void Flush()
        {
            HandleAcquireIfOpenOrFail();

            try
            {
                NativeMethods.FlushFileBuffers(_handle);
            }
            finally
            {
                HandleRelease();
            }
        }

        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count); uint bytesTransferred;

            IntPtr @event = NativeMethods.CreateManualResetEventOrThrow();

            HandleAcquireIfOpenOrFail(); UpdateSettings();
            try
            {
                fixed (byte* ptr = buffer)
                {
                    var overlapped = stackalloc NativeOverlapped[1];
                    overlapped[0].EventHandle = @event;

                    NativeMethods.OverlappedOperation(_handle, @event, ReadTimeout, _closeEventHandle,
                        NativeMethods.ReadFile(_handle, ptr + offset, count, IntPtr.Zero, overlapped),
                        overlapped, out bytesTransferred);
                    return (int)bytesTransferred;
                }
            }
            finally
            {
                HandleRelease();
                NativeMethods.CloseHandle(@event);
            }
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count); uint bytesTransferred;

            IntPtr @event = NativeMethods.CreateManualResetEventOrThrow();

            HandleAcquireIfOpenOrFail(); UpdateSettings();
            try
            {
                fixed (byte* ptr = buffer)
                {
                    var overlapped = stackalloc NativeOverlapped[1];
                    overlapped[0].EventHandle = @event;

                    NativeMethods.OverlappedOperation(_handle, @event, WriteTimeout, _closeEventHandle,
                        NativeMethods.WriteFile(_handle, ptr + offset, count, IntPtr.Zero, overlapped),
                        overlapped, out bytesTransferred);
                    if (bytesTransferred != count) { throw new IOException("Write failed."); }
                }
            }
            finally
            {
                HandleRelease();
                NativeMethods.CloseHandle(@event);
            }
        }

        static void SetDcbDefaults(ref NativeMethods.DCB dcb)
        {
            dcb.fFlags = 0;
            dcb.fBinary = true;
        }

        void UpdateSettings()
        {
            lock (_lock)
            {
                // This assumes the handle is acquired.
                if (!_settingsChanged) { return; }
                _settingsChanged = false;

                var dcb = new NativeMethods.DCB();
                dcb.DCBlength = Marshal.SizeOf(typeof(NativeMethods.DCB));
                if (!NativeMethods.GetCommState(_handle, ref dcb))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw DeviceException.CreateIOException(Device, "Failed to get serial state.", hr);
                }

                int baudRate = _ser.BaudRate;
                int dataBits = _ser.DataBits;
                var parity = _ser.Parity;
                int stopBits = _ser.StopBits;

                SetDcbDefaults(ref dcb);
                dcb.BaudRate = checked((uint)baudRate);
                dcb.ByteSize = checked((byte)_ser.DataBits);
                dcb.Parity = parity == SerialParity.Even ? NativeMethods.EVENPARITY : parity == SerialParity.Odd ? NativeMethods.ODDPARITY : NativeMethods.NOPARITY;
                dcb.StopBits = stopBits == 2 ? NativeMethods.TWOSTOPBITS : NativeMethods.ONESTOPBIT;
                if (!NativeMethods.SetCommState(_handle, ref dcb))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw DeviceException.CreateIOException(Device, "Failed to set serial state.", hr);
                }

                var purgeFlags = NativeMethods.PURGE_RXABORT | NativeMethods.PURGE_RXCLEAR | NativeMethods.PURGE_TXABORT | NativeMethods.PURGE_TXCLEAR;
                if (!NativeMethods.PurgeComm(_handle, purgeFlags))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw DeviceException.CreateIOException(Device, "Failed to purge serial port.", hr);
                }
            }
        }

        public sealed override int BaudRate
        {
            get { return _ser.BaudRate; }
            set { _ser.SetBaudRate(value, _lock, ref _settingsChanged); }
        }

        public sealed override int DataBits
        {
            get { return _ser.DataBits; }
            set { _ser.SetDataBits(value, _lock, ref _settingsChanged); }
        }

        public sealed override SerialParity Parity
        {
            get { return _ser.Parity; }
            set { _ser.SetParity(value, _lock, ref _settingsChanged); }
        }

        public sealed override int StopBits
        {
            get { return _ser.StopBits; }
            set { _ser.SetStopBits(value, _lock, ref _settingsChanged); }
        }

        public sealed override int ReadTimeout
        {
            get;
            set;
        }

        public sealed override int WriteTimeout
        {
            get;
            set;
        }
    }
}
