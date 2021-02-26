#region License
/* Copyright 2012, 2017-2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using HidSharp.Utility;

namespace HidSharp.Platform.MacOS
{
    sealed class MacHidStream : SysHidStream
    {
        Queue<byte[]> _inputQueue;
        Queue<CommonOutputReport> _outputQueue;

        IntPtr _handle;
        IntPtr _readRunLoop;
        Thread _readThread, _writeThread;
        volatile bool _shutdown;

        internal MacHidStream(MacHidDevice device)
            : base(device)
        {
            _inputQueue = new Queue<byte[]>();
            _outputQueue = new Queue<CommonOutputReport>();
            _readThread = new Thread(ReadThread) { IsBackground = true, Name = "HID Reader" };
            _writeThread = new Thread(WriteThread) { IsBackground = true, Name = "HID Writer" };
        }
		
		internal void Init(NativeMethods.io_string_t path)
		{
            IntPtr handle; int retryCount = 0, maxRetries = 10;
            while (true)
            {
                var newPath = path.Clone();
                using (var service = NativeMethods.IORegistryEntryFromPath(0, ref newPath).ToIOObject())
                {
                    string error;

                    if (service.IsSet)
                    {
                        handle = NativeMethods.IOHIDDeviceCreate(IntPtr.Zero, service);
                        if (handle != IntPtr.Zero)
                        {
                            var ret = NativeMethods.IOHIDDeviceOpen(handle);
                            if (ret == NativeMethods.IOReturn.Success) { break; }

                            NativeMethods.CFRelease(handle);

                            // TODO: Only count up if IOReturn is ExclusiveAccess or Offline.
                            error = string.Format("Unable to open HID class device (error {1}): {0}", newPath.ToString(), ret);
                        }
                        else
                        {
                            error = string.Format("HID class device not found: {0}", newPath.ToString());
                        }
                    }
                    else
                    {
                        error = string.Format("HID class device path not found: {0}", newPath.ToString());
                    }

                    if (++retryCount == maxRetries)
                    {
                        throw DeviceException.CreateIOException(Device, error);
                    }

                    HidSharpDiagnostics.Trace("Retrying ({0})", error);
                    Thread.Sleep(100);
                }
            }
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

            while (true)
            {
                var runLoop = _readRunLoop;
                if (runLoop != null) { NativeMethods.CFRunLoopStop(runLoop); }

                try
                {
                    if (_readThread.Join(25)) { break; }
                }
                catch
                {
                    break;
                }
            }

            try { _writeThread.Join(); } catch { }
			HandleRelease();

            base.Dispose(disposing);
        }

		internal override void HandleFree()
		{
			NativeMethods.CFRelease(_handle); _handle = IntPtr.Zero;
		}
		
        void ReadThreadCallback(IntPtr context, NativeMethods.IOReturn result, IntPtr sender,
                                NativeMethods.IOHIDReportType type,
		                        uint reportID, IntPtr report, IntPtr reportLength)
        {
            if (result == NativeMethods.IOReturn.Success && reportLength != IntPtr.Zero)
            {
                if (type == NativeMethods.IOHIDReportType.Input)
                {
                    bool hasReportID = ((MacHidDevice)Device).ReportsUseID; int reportOffset = hasReportID ? 0 : 1;
                    byte[] reportBytes = new byte[checked((int)reportLength + reportOffset)];
                    Marshal.Copy(report, reportBytes, reportOffset, (int)reportLength);

                    var queue = _inputQueue;
                    lock (queue)
                    {
                        if (queue.Count < 512) { queue.Enqueue(reportBytes); Monitor.PulseAll(queue); }
                    }
                }
            }
        }

        void RemovalCallback(IntPtr context, NativeMethods.IOReturn result, IntPtr sender)
        {
            CommonDisconnected(_inputQueue);
        }

        unsafe void ReadThread()
        {
			if (!HandleAcquire()) { return; }
			_readRunLoop = NativeMethods.CFRunLoopGetCurrent();

            try
            {
				var inputCallback = new NativeMethods.IOHIDReportCallback(ReadThreadCallback);
                var removalCallback = new NativeMethods.IOHIDCallback(RemovalCallback);

                byte[] inputReport = new byte[Device.GetMaxInputReportLength()];
                fixed (byte* inputReportBytes = inputReport)
                {
                    NativeMethods.IOHIDDeviceRegisterInputReportCallback(_handle,
                                                                  (IntPtr)inputReportBytes, (IntPtr)inputReport.Length,
                                                                  inputCallback, IntPtr.Zero);
                    NativeMethods.IOHIDDeviceRegisterRemovalCallback(_handle, removalCallback, IntPtr.Zero);
                    NativeMethods.IOHIDDeviceScheduleWithRunLoop(_handle, _readRunLoop, NativeMethods.kCFRunLoopDefaultMode);
                    NativeMethods.CFRunLoopRun();
                    NativeMethods.IOHIDDeviceUnscheduleFromRunLoop(_handle, _readRunLoop, NativeMethods.kCFRunLoopDefaultMode);
                }
				
				GC.KeepAlive(this);
				GC.KeepAlive(inputCallback);
                GC.KeepAlive(removalCallback);
                GC.KeepAlive(_inputQueue);
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
            Throw.If.OutOfRange(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* bufferBytes = buffer)
	            {
                    int reportID = buffer[offset];
                    var hasReportID = ((MacHidDevice)Device).ReportsUseID; int reportOffset = hasReportID ? 0 : 1;
                    var reportPtr = (IntPtr)(bufferBytes + offset + reportOffset);

                    count -= reportOffset;
                    if (count <= 0) { throw new ArgumentException(); }

	                IntPtr reportLength = (IntPtr)count;
	                if (NativeMethods.IOReturn.Success != NativeMethods.IOHIDDeviceGetReport(_handle, NativeMethods.IOHIDReportType.Feature,
	                                                                           (IntPtr)reportID, reportPtr,
	                                                                           ref reportLength))
	
	                {
	                    throw new IOException("GetFeature failed.");
	                }
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
	
						NativeMethods.IOReturn ret;
	                    CommonOutputReport outputReport = _outputQueue.Peek();
	                    try
	                    {
	                        fixed (byte* outputReportBytes = outputReport.Bytes)
	                        {
	                            Monitor.Exit(_outputQueue);
	
	                            try
	                            {
                                    int reportID = outputReport.Bytes[0];
                                    var hasReportID = ((MacHidDevice)Device).ReportsUseID; int reportOffset = hasReportID ? 0 : 1;
                                    var reportPtr = (IntPtr)(outputReportBytes + reportOffset);
                                    int reportLength = outputReport.Bytes.Length - reportOffset;
                                    if (reportLength > 0)
                                    {
                                        ret = NativeMethods.IOHIDDeviceSetReport(_handle,
                                                                          outputReport.Feature ? NativeMethods.IOHIDReportType.Feature : NativeMethods.IOHIDReportType.Output,
                                                                          (IntPtr)reportID, reportPtr, (IntPtr)reportLength);
                                        if (ret == NativeMethods.IOReturn.Success) { outputReport.DoneOK = true; }
                                    }
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

        public override void SetFeature(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, true, Device.GetMaxFeatureReportLength());
        }
    }
}
