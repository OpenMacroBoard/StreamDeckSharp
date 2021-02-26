#region License
/* Copyright 2012, 2015, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Linq;

namespace HidSharp.Platform.MacOS
{
    sealed class MacHidManager : HidManager
    {
        protected override SystemEvents.EventManager CreateEventManager()
        {
            return new SystemEvents.MacOSEventManager();
        }

        protected override void Run(Action readyCallback)
        {
            using (var manager = NativeMethods.IOHIDManagerCreate(IntPtr.Zero).ToCFType())
            {
                RunAssert(manager.IsSet, "HidSharp IOHIDManagerCreate failed.");

                using (var matching = NativeMethods.IOServiceMatching("IOHIDDevice").ToCFType())
                {
                    RunAssert(matching.IsSet, "HidSharp IOServiceMatching failed.");

                    var devicesChangedCallback = new NativeMethods.IOHIDDeviceCallback(DevicesChangedCallback);
                    NativeMethods.IOHIDManagerSetDeviceMatching(manager.Handle, matching.Handle);
                    NativeMethods.IOHIDManagerRegisterDeviceMatchingCallback(manager.Handle, devicesChangedCallback, IntPtr.Zero);
                    NativeMethods.IOHIDManagerRegisterDeviceRemovalCallback(manager.Handle, devicesChangedCallback, IntPtr.Zero);

                    var runLoop = NativeMethods.CFRunLoopGetCurrent();
                    NativeMethods.CFRetain(runLoop);
                    NativeMethods.IOHIDManagerScheduleWithRunLoop(manager, runLoop, NativeMethods.kCFRunLoopDefaultMode);
                    try
                    {
                        readyCallback();
                        NativeMethods.CFRunLoopRun();
                    }
                    finally
                    {
                        NativeMethods.IOHIDManagerUnscheduleFromRunLoop(manager, runLoop, NativeMethods.kCFRunLoopDefaultMode);
                        NativeMethods.CFRelease(runLoop);
                    }

                    GC.KeepAlive(devicesChangedCallback);
                }
            }
        }

        static void DevicesChangedCallback(IntPtr context, NativeMethods.IOReturn result, IntPtr sender, IntPtr device)
        {
            DeviceList.Local.RaiseChanged();
        }

        object[] GetDeviceKeys(string kind)
        {
            var paths = new List<NativeMethods.io_string_t>();

            var matching = NativeMethods.IOServiceMatching(kind).ToCFType(); // Consumed by IOServiceGetMatchingServices, so DON'T Dispose().
            if (matching.IsSet)
            {
                int iteratorObj;
                if (NativeMethods.IOReturn.Success == NativeMethods.IOServiceGetMatchingServices(0, matching, out iteratorObj))
                {
                    using (var iterator = iteratorObj.ToIOObject())
                    {
                        while (true)
                        {
                            using (var handle = NativeMethods.IOIteratorNext(iterator).ToIOObject())
                            {
                                if (!handle.IsSet) { break; }

                                NativeMethods.io_string_t path;
                                if (NativeMethods.IOReturn.Success == NativeMethods.IORegistryEntryGetPath(handle, "IOService", out path))
                                {
                                    paths.Add(path);
                                }
                            }
                        }
                    }
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override object[] GetBleDeviceKeys()
        {
            return new object[0];
        }

        protected override object[] GetHidDeviceKeys()
        {
            return GetDeviceKeys("IOHIDDevice");
        }

        protected override object[] GetSerialDeviceKeys()
        {
            return GetDeviceKeys("IOSerialBSDClient");
        }

        protected override bool TryCreateBleDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }

        protected override bool TryCreateHidDevice(object key, out Device device)
        {
            device = MacHidDevice.TryCreate((NativeMethods.io_string_t)key);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            device = MacSerialDevice.TryCreate((NativeMethods.io_string_t)key);
            return device != null;
        }

        public override string FriendlyName
        {
            get { return "Mac OS HID"; }
        }

        public override bool IsSupported
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    try
                    {
                        IntPtr major; NativeMethods.OSErr majorErr = NativeMethods.Gestalt(NativeMethods.OSType.gestaltSystemVersionMajor, out major);
                        IntPtr minor; NativeMethods.OSErr minorErr = NativeMethods.Gestalt(NativeMethods.OSType.gestaltSystemVersionMinor, out minor);
                        if (majorErr == NativeMethods.OSErr.noErr && minorErr == NativeMethods.OSErr.noErr)
                        {
                            return (long)major >= 10 || ((long)major == 10 && (long)minor >= 6);
                        }
                    }
                    catch
                    {

                    }
                }

                return false;
            }
        }
    }
}
