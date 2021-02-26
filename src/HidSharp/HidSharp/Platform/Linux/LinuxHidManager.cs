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
using System.Linq;

namespace HidSharp.Platform.Linux
{
    sealed class LinuxHidManager : HidManager
    {
        protected override SystemEvents.EventManager CreateEventManager()
        {
            return new SystemEvents.LinuxEventManager();
        }

        protected override void Run(Action readyCallback)
        {
            IntPtr udev = NativeMethodsLibudev.Instance.udev_new();
            RunAssert(udev != IntPtr.Zero, "HidSharp udev_new failed.");

            try
            {
                IntPtr monitor = NativeMethodsLibudev.Instance.udev_monitor_new_from_netlink(udev, "udev");
                RunAssert(monitor != IntPtr.Zero, "HidSharp udev_monitor_new_from_netlink failed.");

                try
                {
                    int ret;

                    ret = NativeMethodsLibudev.Instance.udev_monitor_filter_add_match_subsystem_devtype(monitor, "hid", null);
                    RunAssert(ret >= 0, "HidSharp udev_monitor_failed_add_match_subsystem_devtype failed.");

                    ret = NativeMethodsLibudev.Instance.udev_monitor_enable_receiving(monitor);
                    RunAssert(ret >= 0, "HidSharp udev_monitor_enable_receiving failed.");

                    int fd = NativeMethodsLibudev.Instance.udev_monitor_get_fd(monitor);
                    RunAssert(fd >= 0, "HidSharp udev_monitor_get_fd failed.");

                    var fds = new NativeMethods.pollfd[1];
                    fds[0].fd = fd;
                    fds[0].events = NativeMethods.pollev.IN;

                    readyCallback();
                    while (true)
                    {
                        ret = NativeMethods.retry(() => NativeMethods.poll(fds, (IntPtr)1, -1));
                        if (ret < 0) { break; }

                        if (ret == 1)
                        {
                            if (0 != (fds[0].revents & (NativeMethods.pollev.ERR | NativeMethods.pollev.HUP | NativeMethods.pollev.NVAL))) { break; }
                            if (0 != (fds[0].revents & NativeMethods.pollev.IN))
                            {
                                IntPtr device = NativeMethodsLibudev.Instance.udev_monitor_receive_device(monitor);
                                if (device != null)
                                {
                                    NativeMethodsLibudev.Instance.udev_device_unref(device);

                                    DeviceList.Local.RaiseChanged();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    NativeMethodsLibudev.Instance.udev_monitor_unref(monitor);
                }
            }
            finally
            {
                NativeMethodsLibudev.Instance.udev_unref(udev);
            }
        }

        protected override object[] GetBleDeviceKeys()
        {
            return new object[0];
        }

        protected override object[] GetHidDeviceKeys()
        {
            return GetDeviceKeys("hidraw");
        }

        protected override object[] GetSerialDeviceKeys()
        {
            //return GetDeviceKeys("tty"); // TODO: Find proper DevicePaths by enumerating tty.
            try
            {
                return Directory.GetFiles("/dev/").Where(name =>
                    name.StartsWith("/dev/ttyACM") || name.StartsWith("/dev/ttyUSB")
                    ).Cast<object>().ToArray();
            }
            catch
            {
                return new object[0];
            }
        }

        object[] GetDeviceKeys(string subsystem)
        {
            var paths = new List<string>();

            IntPtr udev = NativeMethodsLibudev.Instance.udev_new();
            if (IntPtr.Zero != udev)
            {
                try
                {
                    IntPtr enumerate = NativeMethodsLibudev.Instance.udev_enumerate_new(udev);
                    if (IntPtr.Zero != enumerate)
                    {
                        try
                        {
                            if (0 == NativeMethodsLibudev.Instance.udev_enumerate_add_match_subsystem(enumerate, subsystem) &&
                                0 == NativeMethodsLibudev.Instance.udev_enumerate_scan_devices(enumerate))
                            {
                                IntPtr entry;
                                for (entry = NativeMethodsLibudev.Instance.udev_enumerate_get_list_entry(enumerate); entry != IntPtr.Zero;
                                     entry = NativeMethodsLibudev.Instance.udev_list_entry_get_next(entry))
                                {
                                    string syspath = NativeMethodsLibudev.Instance.udev_list_entry_get_name(entry);
                                    if (syspath != null) { paths.Add(syspath); }
                                }
                            }
                        }
                        finally
                        {
                            NativeMethodsLibudev.Instance.udev_enumerate_unref(enumerate);
                        }
                    }
                }
                finally
                {
                    NativeMethodsLibudev.Instance.udev_unref(udev);
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override bool TryCreateBleDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }

        protected override bool TryCreateHidDevice(object key, out Device device)
        {
            device = LinuxHidDevice.TryCreate((string)key);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            device = LinuxSerialDevice.TryCreate((string)key); return true;
        }

        public override string FriendlyName
        {
            get
            {
                var instance = NativeMethodsLibudev.Instance;
                return "Linux hidraw (" + (instance != null ? instance.FriendlyName : "?") + ")";
            }
        }

        public override bool IsSupported
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    try
                    {
                        string sysname; Version release;
                        if (NativeMethods.uname(out sysname, out release))
                        {
                            if (sysname == "Linux" && release >= new Version(2, 6, 36))
                            {
                                if (NativeMethodsLibudev.Instance != null)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {

                    }
                }

                return false;
            }
        }
    }
}
