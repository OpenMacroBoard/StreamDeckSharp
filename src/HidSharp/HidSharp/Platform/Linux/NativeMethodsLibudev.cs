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

namespace HidSharp.Platform.Linux
{
    abstract class NativeMethodsLibudev
    {
        static NativeMethodsLibudev()
        {
            foreach (var instance in new NativeMethodsLibudev[] { new NativeMethodsLibudev1(), new NativeMethodsLibudev0() })
            {
                try
                {
                    IntPtr udev = instance.udev_new();
                    if (IntPtr.Zero != udev)
                    {
                        instance.udev_unref(udev);
                        Instance = instance; break;
                    }
                }
                catch
                {

                }
            }
        }

        public static NativeMethodsLibudev Instance
        {
            get;
            private set;
        }

        public abstract string FriendlyName
        {
            get;
        }

        public abstract IntPtr udev_new();
        
        public abstract IntPtr udev_ref(IntPtr udev);
        
        public abstract void udev_unref(IntPtr udev);
        
        public abstract IntPtr udev_monitor_new_from_netlink(IntPtr udev, string name);
        
        public abstract void udev_monitor_unref(IntPtr monitor);
        
        public abstract int udev_monitor_filter_add_match_subsystem_devtype(IntPtr monitor, string subsystem, string devtype);
        
        public abstract int udev_monitor_enable_receiving(IntPtr monitor);
        
        public abstract int udev_monitor_get_fd(IntPtr monitor);
        
        public abstract IntPtr udev_monitor_receive_device(IntPtr monitor);
        
        public abstract IntPtr udev_enumerate_new(IntPtr udev);
        
        public abstract IntPtr udev_enumerate_ref(IntPtr enumerate);
        
        public abstract void udev_enumerate_unref(IntPtr enumerate);
        
        public abstract int udev_enumerate_add_match_subsystem(IntPtr enumerate, string subsystem);
        
        public abstract int udev_enumerate_scan_devices(IntPtr enumerate);
        
        public abstract IntPtr udev_enumerate_get_list_entry(IntPtr enumerate);
        
        public abstract IntPtr udev_list_entry_get_next(IntPtr entry);

        public abstract string udev_list_entry_get_name(IntPtr entry);
        
        public abstract IntPtr udev_device_new_from_syspath(IntPtr udev, string syspath);

        public abstract IntPtr udev_device_ref(IntPtr device);

        public abstract void udev_device_unref(IntPtr device);

        public abstract string udev_device_get_devnode(IntPtr device);

        public abstract IntPtr udev_device_get_parent_with_subsystem_devtype(IntPtr device, string subsystem, string devtype);

        public abstract string udev_device_get_sysattr_value(IntPtr device, string sysattr);

        public abstract int udev_device_get_is_initialized(IntPtr device);
    }
}
