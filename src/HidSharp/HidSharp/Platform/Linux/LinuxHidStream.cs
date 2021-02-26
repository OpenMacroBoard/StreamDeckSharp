#region License
/* Copyright 2012, 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Linux
{
    sealed class LinuxHidStream : SysHidStream
    {
		Queue<byte[]> _inputQueue;
		Queue<CommonOutputReport> _outputQueue;
		
		int _handle;
		Thread _readThread, _writeThread;
		volatile bool _shutdown;
		
        internal LinuxHidStream(LinuxHidDevice device)
            : base(device)
        {
			_inputQueue = new Queue<byte[]>();
			_outputQueue = new Queue<CommonOutputReport>();
			_handle = -1;
            _readThread = new Thread(ReadThread) { IsBackground = true, Name = "HID Reader" };
            _writeThread = new Thread(WriteThread) { IsBackground = true, Name = "HID Writer" };
        }
		
		internal static int DeviceHandleFromPath(string path, HidDevice hidDevice, NativeMethods.oflag oflag)
		{
            IntPtr udev = NativeMethodsLibudev.Instance.udev_new();
			if (IntPtr.Zero != udev)
			{
				try
				{
					IntPtr device = NativeMethodsLibudev.Instance.udev_device_new_from_syspath(udev, path);
					if (IntPtr.Zero != device)
					{
						try
						{
                            string devnode = NativeMethodsLibudev.Instance.udev_device_get_devnode(device);
							if (devnode != null)
							{
								int handle = NativeMethods.retry(() => NativeMethods.open(devnode, oflag));
								if (handle < 0)
								{
									var error = (NativeMethods.error)Marshal.GetLastWin32Error();
									if (error == NativeMethods.error.EACCES)
									{
                                        throw DeviceException.CreateUnauthorizedAccessException(hidDevice, "Not permitted to open HID class device at " + devnode + ".");
									}
									else
									{
                                        throw DeviceException.CreateIOException(hidDevice, "Unable to open HID class device (" + error.ToString() + ").");
									}
								}
								return handle;
							}
						}
						finally
						{
                            NativeMethodsLibudev.Instance.udev_device_unref(device);
						}
					}
				}
				finally
				{
                    NativeMethodsLibudev.Instance.udev_unref(udev);
				}
			}
			
			throw new FileNotFoundException("HID class device not found.");
		}
		
        internal void Init(string path)
        {
			int handle;
            handle = DeviceHandleFromPath(path, Device, NativeMethods.oflag.RDWR | NativeMethods.oflag.NONBLOCK);
			
			_handle = handle;
			HandleInitAndOpen();
			
			_readThread.Start();
			_writeThread.Start();
        }
		
        protected override void Dispose(bool disposing)
        {
			if (!HandleClose()) { return; }
			
            _shutdown = true;
            try { lock (_inputQueue) { Monitor.PulseAll(_inputQueue); } } catch { }
			try { lock (_outputQueue) { Monitor.PulseAll(_outputQueue); } } catch { }

            try { _readThread.Join(); } catch { }
            try { _writeThread.Join(); } catch { }

			HandleRelease();

            base.Dispose(disposing);
		}
		
		internal override void HandleFree()
		{
			NativeMethods.retry(() => NativeMethods.close(_handle)); _handle = -1;
		}
		
		unsafe void ReadThread()
		{
			if (!HandleAcquire()) { return; }
			
			try
			{
				lock (_inputQueue)
				{
                    var pfd = new NativeMethods.pollfd();
					pfd.fd = _handle;
					pfd.events = NativeMethods.pollev.IN;
						
					while (!_shutdown)
					{
					tryReadAgain:
						Monitor.Exit(_inputQueue);

                        int ret;
						try { ret = NativeMethods.poll(ref pfd, (IntPtr)1, 250); }
						finally { Monitor.Enter(_inputQueue); }
                        if (ret != 1) { continue; }

                        if (0 != (pfd.revents & (NativeMethods.pollev.ERR | NativeMethods.pollev.HUP | NativeMethods.pollev.NVAL)))
                        {
                            break;
                        }

						if (0 != (pfd.revents & NativeMethods.pollev.IN))
						{
                            // Linux doesn't provide a Report ID if the device doesn't use one.
                            int inputLength = Device.GetMaxInputReportLength();
                            if (inputLength > 0 && !((LinuxHidDevice)Device).ReportsUseID) { inputLength--; }

                            byte[] inputReport = new byte[inputLength];
							fixed (byte* inputBytes = inputReport)
							{
                                var inputBytesPtr = (IntPtr)inputBytes;
								IntPtr length = NativeMethods.retry(() => NativeMethods.read
									                            (_handle, inputBytesPtr, (UIntPtr)inputReport.Length));
								if ((long)length < 0)
								{
                                    var error = (NativeMethods.error)Marshal.GetLastWin32Error();
									if (error != NativeMethods.error.EAGAIN) { break; }
									goto tryReadAgain;
								}

								Array.Resize(ref inputReport, (int)length); // No Report ID? First byte becomes Report ID 0.
                                if (!((LinuxHidDevice)Device).ReportsUseID) { inputReport = new byte[1].Concat(inputReport).ToArray(); }
								_inputQueue.Enqueue(inputReport); Monitor.PulseAll(_inputQueue);
							}
						}
					}

                    CommonDisconnected(_inputQueue);
				}
			}
			finally
			{
				HandleRelease();
			}
		}
		
        public override int Read(byte[] buffer, int offset, int count)
        {
            return CommonRead(buffer, offset, count, _inputQueue);
        }

        public unsafe override void GetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count).False(count >= 2);

            HandleAcquireIfOpenOrFail();
            try
            {
                byte reportID = buffer[offset];

                fixed (byte* ptr = buffer)
                {
                    buffer[offset + 1] = reportID;
                    int bytes = NativeMethods.ioctl(_handle, NativeMethods.HIDIOCGFEATURE(count - 1), (IntPtr)(ptr + offset + 1));
                    if (bytes < 0) { throw new IOException("GetFeature failed."); }
                    Array.Clear(buffer, 1 + bytes, count - (1 + bytes));
                }
            }
            finally
            {
                HandleRelease();
            }
        }

		unsafe void WriteThread()
		{
			if (!HandleAcquire()) { return; }
			
			try
			{
				lock (_outputQueue)
				{
					while (true)
					{
						while (!_shutdown && _outputQueue.Count == 0) { Monitor.Wait(_outputQueue); }
						if (_shutdown) { break; }

						CommonOutputReport outputReport = _outputQueue.Peek();

                        byte[] outputBytesRaw = outputReport.Bytes;
                        // Linux doesn't expect a Report ID if the device doesn't use one.
                        //if (!((LinuxHidDevice)Device).ReportsUseID && outputBytesRaw.Length > 0) { outputBytesRaw = outputBytesRaw.Skip(1).ToArray(); }
                        // BUGFIX: Yes, it does. But only on write(). (HIDSharp 2.0.2)

						try
						{
							fixed (byte* outputBytes = outputBytesRaw)
							{
								// hidraw is apparently blocking for output, even when O_NONBLOCK is used.
								// See for yourself at drivers/hid/hidraw.c...
                                IntPtr length;
                                Monitor.Exit(_outputQueue);
                                try
                                {
                                    var outputBytesPtr = (IntPtr)outputBytes;
                                    length = NativeMethods.retry(() => NativeMethods.write
                                                            (_handle, outputBytesPtr, (UIntPtr)outputBytesRaw.Length));
                                    if ((long)length == outputBytesRaw.Length) { outputReport.DoneOK = true; }
                                }
                                finally
                                {
                                    Monitor.Enter(_outputQueue);
                                }
							}
						}
						finally
						{
							_outputQueue.Dequeue();
                            outputReport.Done = true;
							Monitor.PulseAll(_outputQueue);
						}	
					}
				}
			}
			finally
			{
				HandleRelease();
			}
		}
		
        public override void Write(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, false, Device.GetMaxOutputReportLength());
        }

        public unsafe override void SetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            HandleAcquireIfOpenOrFail();
            try
            {
                fixed (byte* ptr = buffer)
                {
                    if (NativeMethods.ioctl(_handle, NativeMethods.HIDIOCSFEATURE(count), (IntPtr)(ptr + offset)) < 0)
                        { throw new IOException("SetFeature failed."); }
                }
            }
            finally
            {
                HandleRelease();
            }
        }
    }
}
