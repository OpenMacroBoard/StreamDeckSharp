#region License
/* Copyright 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp
{
    public delegate bool DeviceFilter(Device device);

    public static class DeviceFilterHelper
    {
        public static bool MatchHidDevices(Device device, int? vendorID = null, int? productID = null, int? releaseNumberBcd = null, string serialNumber = null)
        {
            var hidDevice = device as HidDevice;
            if (hidDevice != null)
            {
                int vid = vendorID ?? -1, pid = productID ?? -1, ver = releaseNumberBcd ?? -1;

                if ((vid < 0 || hidDevice.VendorID == vendorID) &&
                    (pid < 0 || hidDevice.ProductID == productID) &&
                    (ver < 0 || hidDevice.ReleaseNumberBcd == releaseNumberBcd))
                {
                    try
                    {
                        if (string.IsNullOrEmpty(serialNumber) || hidDevice.GetSerialNumber() == serialNumber) { return true; }
                    }
                    catch
                    {

                    }
                }
            }

            return false;
        }

        public static bool MatchSerialDevices(Device device, string portName = null)
        {
            var serialDevice = device as SerialDevice;
            if (serialDevice != null)
            {
                if (string.IsNullOrEmpty(portName) || serialDevice.DevicePath == portName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
