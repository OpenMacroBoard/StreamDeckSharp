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

using System;

namespace HidSharp
{
    /// <summary>
    /// Specifies the <see cref="Device"/>'s low-level implementation.
    /// </summary>
    public static class ImplementationDetail
    {
        /// <summary>
        /// The device is running on Windows.
        /// </summary>
        public static Guid Windows { get; private set; }

        /// <summary>
        /// The device is running on a Mac.
        /// </summary>
        public static Guid MacOS { get; private set; }

        /// <summary>
        /// The device is running on Linux.
        /// </summary>
        public static Guid Linux { get; private set; }

        /// <summary>
        /// The device is a Bluetooth Low Energy device.
        /// </summary>
        public static Guid BleDevice { get; private set; }

        /// <summary>
        /// The device is a HID device.
        /// </summary>
        public static Guid HidDevice { get; private set; }

        /// <summary>
        /// The device is a serial device.
        /// </summary>
        public static Guid SerialDevice { get; private set; }

        /// <summary>
        /// The device is implemented using the Linux hidraw API.
        /// </summary>
        public static Guid HidrawApi { get; private set; }

        static ImplementationDetail()
        {
            Windows = new Guid("{3540D886-E329-419F-8033-1D7355D53A7E}");
            MacOS = new Guid("{9FE992E5-F804-41B6-A35F-3B60F7CAC9E2}");
            Linux = new Guid("{A4123219-6BC8-49B7-84D3-699A66373109}");

            BleDevice = new Guid("{AAFD1479-29A0-42B8-A0A9-5C88A18B5504}");
            HidDevice = new Guid("{DFF209D7-131E-4958-8F47-C23DAC7B62DA}");
            SerialDevice = new Guid("{45A96DA9-AA48-4BF7-978D-A845F185F38C}");

            HidrawApi = new Guid("{1199D7C6-F99F-471F-9730-B16BA615938F}");
        }
    }
}
