#region License
/* Copyright 2010, 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HidSharp
{
    /// <exclude />
    [ComVisible(true), Guid("CD7CBD7D-7204-473c-AA2A-2B9622CFC6CC")]
    [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
    public class HidDeviceLoader
    {
        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public HidDeviceLoader()
        {

        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable GetDevicesVB()
        {
            return DeviceList.Local.GetHidDevices();
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<HidDevice> GetDevices()
        {
            return DeviceList.Local.GetHidDevices();
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<HidDevice> GetDevices(int? vendorID = null, int? productID = null, int? productVersion = null, string serialNumber = null)
        {
            return DeviceList.Local.GetHidDevices(vendorID, productID, productVersion, serialNumber);
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public HidDevice GetDeviceOrDefault(int? vendorID = null, int? productID = null, int? productVersion = null, string serialNumber = null)
        {
            return DeviceList.Local.GetHidDeviceOrNull(vendorID, productID, productVersion, serialNumber);
        }
    }
}
