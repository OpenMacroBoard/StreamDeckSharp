#region License
/* Copyright 2012-2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    sealed class WinHidStream : SysHidStream
    {
        object _readSync = new object(), _writeSync = new object();
        byte[] _readBuffer, _writeBuffer;
        IntPtr _handle, _closeEventHandle;

        internal WinHidStream(WinHidDevice device)
            : base(device)
        {
            _closeEventHandle = NativeMethods.CreateManualResetEventOrThrow();
        }

        ~WinHidStream()
        {
			Close();
            NativeMethods.CloseHandle(_closeEventHandle);
        }

        internal void Init(string path)
        {
            IntPtr handle = NativeMethods.CreateFileFromDevice(path, NativeMethods.EFileAccess.Read | NativeMethods.EFileAccess.Write, NativeMethods.EFileShare.Read | NativeMethods.EFileShare.Write);
            if (handle == (IntPtr)(-1))
            {
                throw DeviceException.CreateIOException(Device, "Unable to open HID class device (" + path + ").");
            }

            int maxInputBuffers = Environment.OSVersion.Version >= new Version(5, 1) ? 512 : 200; // Windows 2000 supports 200. Windows XP supports 512.
            if (!NativeMethods.HidD_SetNumInputBuffers(handle, maxInputBuffers))
            {
                NativeMethods.CloseHandle(handle);
                throw new IOException("Failed to set input buffers.", new Win32Exception());
            }

			_handle = handle;
			HandleInitAndOpen();
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

        public unsafe override void GetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* ptr = buffer)
	            {
	                if (!NativeMethods.HidD_GetFeature(_handle, ptr + offset, count))
	                    { throw new IOException("GetFeature failed.", new Win32Exception()); }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        // Buffer needs to be big enough for the largest report, plus a byte
        // for the Report ID.
        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count); uint bytesTransferred;
            IntPtr @event = NativeMethods.CreateManualResetEventOrThrow();
			
			HandleAcquireIfOpenOrFail();
            try
            {
				lock (_readSync)
				{
	                int minIn = Device.GetMaxInputReportLength();
                    if (minIn <= 0) { throw new IOException("Can't read from this device."); }
                    if (_readBuffer == null || _readBuffer.Length < Math.Max(count, minIn)) { Array.Resize(ref _readBuffer, Math.Max(count, minIn)); }
	
	                fixed (byte* ptr = _readBuffer)
	                {
                        var overlapped = stackalloc NativeOverlapped[1];
                        overlapped[0].EventHandle = @event;
                        
                        NativeMethods.OverlappedOperation(_handle, @event, ReadTimeout, _closeEventHandle,
                            NativeMethods.ReadFile(_handle, ptr, Math.Max(count, minIn), IntPtr.Zero, overlapped),
                            overlapped, out bytesTransferred);

	                    if (count > (int)bytesTransferred) { count = (int)bytesTransferred; }
	                    Array.Copy(_readBuffer, 0, buffer, offset, count);
	                    return count;
	                }
				}
            }
            finally
            {
				HandleRelease();
                NativeMethods.CloseHandle(@event);
            }
        }

        public unsafe override void SetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* ptr = buffer)
	            {
	                if (!NativeMethods.HidD_SetFeature(_handle, ptr + offset, count))
	                    { throw new IOException("SetFeature failed.", new Win32Exception()); }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count); uint bytesTransferred;
            IntPtr @event = NativeMethods.CreateManualResetEventOrThrow();

			HandleAcquireIfOpenOrFail();
            try
            {
				lock (_writeSync)
				{
	                int minOut = Device.GetMaxOutputReportLength();
                    if (minOut <= 0) { throw new IOException("Can't write to this device."); }
                    if (_writeBuffer == null || _writeBuffer.Length < Math.Max(count, minOut)) { Array.Resize(ref _writeBuffer, Math.Max(count, minOut)); }
	                Array.Copy(buffer, offset, _writeBuffer, 0, count);

                    if (count < minOut)
                    {
                        Array.Clear(_writeBuffer, count, minOut - count);
                        count = minOut;
                    }
	
	                fixed (byte* ptr = _writeBuffer)
	                {
	                    int offset0 = 0;
	                    while (count > 0)
	                    {
                            var overlapped = stackalloc NativeOverlapped[1];
                            overlapped[0].EventHandle = @event;

                            NativeMethods.OverlappedOperation(_handle, @event, WriteTimeout, _closeEventHandle,
	                            NativeMethods.WriteFile(_handle, ptr + offset0, Math.Min(minOut, count), IntPtr.Zero, overlapped),
	                            overlapped, out bytesTransferred);
	                        count -= (int)bytesTransferred; offset0 += (int)bytesTransferred;
	                    }
	                }
				}
            }
            finally
            {
				HandleRelease();
                NativeMethods.CloseHandle(@event);
            }
        }
    }
}
