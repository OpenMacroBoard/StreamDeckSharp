#region License
/* Copyright 2016 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Linux
{
    sealed class NativeMethodsLibudev0 : NativeMethodsLibudev
    {
        const string libudev = "libudev.so.0";

        public override string FriendlyName
        {
            get { return libudev; }
        }

        [DllImport(libudev, EntryPoint = "udev_new")]
        static extern IntPtr native_udev_new();
        public override IntPtr udev_new()
        {
            return native_udev_new();
        }

        [DllImport(libudev, EntryPoint = "udev_ref")]
        static extern IntPtr native_udev_ref(IntPtr udev);
        public override IntPtr udev_ref(IntPtr udev)
        {
            return native_udev_ref(udev);
        }

        [DllImport(libudev, EntryPoint = "udev_unref")]
        static extern void native_udev_unref(IntPtr udev);
        public override void udev_unref(IntPtr udev)
        {
            native_udev_unref(udev);
        }

        [DllImport(libudev, EntryPoint = "udev_monitor_new_from_netlink")]
        static extern IntPtr native_udev_monitor_new_from_netlink(IntPtr udev,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);
        public override IntPtr udev_monitor_new_from_netlink(IntPtr udev, string name)
        {
            return native_udev_monitor_new_from_netlink(udev, name);
        }

        [DllImport(libudev, EntryPoint = "udev_monitor_unref")]
        static extern void native_udev_monitor_unref(IntPtr monitor);
        public override void udev_monitor_unref(IntPtr monitor)
        {
            native_udev_monitor_unref(monitor);
        }

        [DllImport(libudev, EntryPoint = "udev_monitor_filter_add_match_subsystem_devtype")]
        static extern int native_udev_monitor_filter_add_match_subsystem_devtype(IntPtr monitor,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string subsystem,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string devtype);
        public override int udev_monitor_filter_add_match_subsystem_devtype(IntPtr monitor, string subsystem, string devtype)
        {
            return native_udev_monitor_filter_add_match_subsystem_devtype(monitor, subsystem, devtype);
        }

        [DllImport(libudev, EntryPoint = "udev_monitor_enable_receiving")]
        static extern int native_udev_monitor_enable_receiving(IntPtr monitor);
        public override int udev_monitor_enable_receiving(IntPtr monitor)
        {
            return native_udev_monitor_enable_receiving(monitor);
        }

        [DllImport(libudev, EntryPoint = "udev_monitor_get_fd")]
        static extern int native_udev_monitor_get_fd(IntPtr monitor);
        public override int udev_monitor_get_fd(IntPtr monitor)
        {
            return native_udev_monitor_get_fd(monitor);
        }

        [DllImport(libudev, EntryPoint = "udev_monitor_receive_device")]
        static extern IntPtr native_udev_monitor_receive_device(IntPtr monitor);
        public override IntPtr udev_monitor_receive_device(IntPtr monitor)
        {
            return native_udev_monitor_receive_device(monitor);
        }

        [DllImport(libudev, EntryPoint = "udev_enumerate_new")]
        static extern IntPtr native_udev_enumerate_new(IntPtr udev);
        public override IntPtr udev_enumerate_new(IntPtr udev)
        {
            return native_udev_enumerate_new(udev);
        }

        [DllImport(libudev, EntryPoint = "udev_enumerate_ref")]
        static extern IntPtr native_udev_enumerate_ref(IntPtr enumerate);
        public override IntPtr udev_enumerate_ref(IntPtr enumerate)
        {
            return native_udev_enumerate_ref(enumerate);
        }

        [DllImport(libudev, EntryPoint = "udev_enumerate_unref")]
        static extern void native_udev_enumerate_unref(IntPtr enumerate);
        public override void udev_enumerate_unref(IntPtr enumerate)
        {
            native_udev_enumerate_unref(enumerate);
        }

        [DllImport(libudev, EntryPoint = "udev_enumerate_add_match_subsystem")]
        static extern int native_udev_enumerate_add_match_subsystem(IntPtr enumerate,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string subsystem);
        public override int udev_enumerate_add_match_subsystem(IntPtr enumerate, string subsystem)
        {
            return native_udev_enumerate_add_match_subsystem(enumerate, subsystem);
        }

        [DllImport(libudev, EntryPoint = "udev_enumerate_scan_devices")]
        static extern int native_udev_enumerate_scan_devices(IntPtr enumerate);
        public override int udev_enumerate_scan_devices(IntPtr enumerate)
        {
            return native_udev_enumerate_scan_devices(enumerate);
        }

        [DllImport(libudev, EntryPoint = "udev_enumerate_get_list_entry")]
        static extern IntPtr native_udev_enumerate_get_list_entry(IntPtr enumerate);
        public override IntPtr udev_enumerate_get_list_entry(IntPtr enumerate)
        {
            return native_udev_enumerate_get_list_entry(enumerate);
        }

        [DllImport(libudev, EntryPoint = "udev_list_entry_get_next")]
        static extern IntPtr native_udev_list_entry_get_next(IntPtr entry);
        public override IntPtr udev_list_entry_get_next(IntPtr entry)
        {
            return native_udev_list_entry_get_next(entry);
        }

        [DllImport(libudev, EntryPoint = "udev_list_entry_get_name")]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        static extern string native_udev_list_entry_get_name(IntPtr entry);
        public override string udev_list_entry_get_name(IntPtr entry)
        {
            return native_udev_list_entry_get_name(entry);
        }

        [DllImport(libudev, EntryPoint = "udev_device_new_from_syspath")]
        static extern IntPtr native_udev_device_new_from_syspath(IntPtr udev,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string syspath);
        public override IntPtr udev_device_new_from_syspath(IntPtr udev, string syspath)
        {
            return native_udev_device_new_from_syspath(udev, syspath);
        }

        [DllImport(libudev, EntryPoint = "udev_device_ref")]
        static extern IntPtr native_udev_device_ref(IntPtr device);
        public override IntPtr udev_device_ref(IntPtr device)
        {
            return native_udev_device_ref(device);
        }

        [DllImport(libudev, EntryPoint = "udev_device_unref")]
        static extern void native_udev_device_unref(IntPtr device);
        public override void udev_device_unref(IntPtr device)
        {
            native_udev_device_unref(device);
        }

        [DllImport(libudev, EntryPoint = "udev_device_get_devnode")]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        static extern string native_udev_device_get_devnode(IntPtr device);
        public override string udev_device_get_devnode(IntPtr device)
        {
            return native_udev_device_get_devnode(device);
        }

        [DllImport(libudev, EntryPoint = "udev_device_get_parent_with_subsystem_devtype")]
        static extern IntPtr native_udev_device_get_parent_with_subsystem_devtype(IntPtr device,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string subsystem,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string devtype);
        public override IntPtr udev_device_get_parent_with_subsystem_devtype(IntPtr device, string subsystem, string devtype)
        {
            return native_udev_device_get_parent_with_subsystem_devtype(device, subsystem, devtype);
        }

        [DllImport(libudev, EntryPoint = "udev_device_get_sysattr_value")]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        static extern string native_udev_device_get_sysattr_value(IntPtr device,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string sysattr);
        public override string udev_device_get_sysattr_value(IntPtr device, string sysattr)
        {
            return native_udev_device_get_sysattr_value(device, sysattr);
        }

        [DllImport(libudev, EntryPoint = "udev_device_get_is_initialized")]
        static extern int native_udev_device_get_is_initialized(IntPtr device);
        public override int udev_device_get_is_initialized(IntPtr device)
        {
            return native_udev_device_get_is_initialized(device);
        }
    }
}
