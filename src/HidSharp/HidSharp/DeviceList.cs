#region License
/* Copyright 2015-2016, 2018-2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using HidSharp.Experimental;

namespace HidSharp
{
    /// <summary>
    /// Provides a list of all available devices.
    /// </summary>
    [ComVisible(true), Guid("80614F94-0742-4DE4-8AE9-DF9D55F870F2")]
    public abstract class DeviceList
    {
        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public static event EventHandler<DeviceListChangedEventArgs> DeviceListChanged;

        /// <summary>
        /// Occurs when a device is connected or disconnected.
        /// </summary>
        public event EventHandler<DeviceListChangedEventArgs> Changed;

        static DeviceList()
        {
            Local = new LocalDeviceList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        protected DeviceList()
        {

        }

        /*
        public abstract BleDiscovery BeginBleDiscovery();
        */

        public virtual IEnumerable<Device> GetDevices(DeviceTypes types)
        {
            // Improve performance by implementing this override.
            return GetAllDevices().Where(device =>
                {
                    if (device is HidDevice && 0 != (types & DeviceTypes.Hid)) { return true; }
                    if (device is SerialDevice && 0 != (types & DeviceTypes.Serial)) { return true; }
                    if (device is BleDevice && 0 != (types & DeviceTypes.Ble)) { return true; }
                    return false;
                });
        }

        public IEnumerable<Device> GetDevices(DeviceTypes types, DeviceFilter filter)
        {
            Throw.If.Null(filter, "filter");
            return GetDevices(types).Where(device => filter(device));
        }

        /// <summary>
        /// Gets a list of all connected BLE devices.
        /// </summary>
        /// <returns>The device list.</returns>
        public IEnumerable<BleDevice> GetBleDevices()
        {
            return GetDevices(DeviceTypes.Ble).Cast<BleDevice>();
        }

        /// <summary>
        /// Gets a list of all connected HID devices.
        /// </summary>
        /// <returns>The device list.</returns>
        public IEnumerable<HidDevice> GetHidDevices()
        {
            return GetDevices(DeviceTypes.Hid).Cast<HidDevice>();
        }

        /// <summary>
        /// Gets a list of connected HID devices, filtered by some criteria.
        /// </summary>
        /// <param name="vendorID">The vendor ID, or null to not filter by vendor ID.</param>
        /// <param name="productID">The product ID, or null to not filter by product ID.</param>
        /// <param name="releaseNumberBcd">The device release number in binary-coded decimal, or null to not filter by device release number.</param>
        /// <param name="serialNumber">The serial number, or null to not filter by serial number.</param>
        /// <returns>The filtered device list.</returns>
        public IEnumerable<HidDevice> GetHidDevices(int? vendorID = null, int? productID = null, int? releaseNumberBcd = null, string serialNumber = null)
        {
            return GetDevices(DeviceTypes.Hid, d => DeviceFilterHelper.MatchHidDevices(d, vendorID, productID, releaseNumberBcd, serialNumber)).Cast<HidDevice>();
        }

        /// <summary>
        /// Gets a list of all connected serial devices.
        /// </summary>
        /// <returns>The device list.</returns>
        public IEnumerable<SerialDevice> GetSerialDevices()
        {
            return GetDevices(DeviceTypes.Serial).Cast<SerialDevice>();
        }

        /// <summary>
        /// Gets a list of all connected HID, BLE, and serial devices.
        /// </summary>
        /// <returns>The device list.</returns>
        public abstract IEnumerable<Device> GetAllDevices();

        /// <summary>
        /// Gets a list of connected devices, filtered by some criteria.
        /// </summary>
        /// <param name="filter">The filter criteria.</param>
        /// <returns>The filtered device list.</returns>
        public IEnumerable<Device> GetAllDevices(DeviceFilter filter)
        {
            Throw.If.Null(filter, "filter");
            return GetAllDevices().Where(device => filter(device));
        }

        /// <summary>
        /// Gets the first connected HID device that matches specified criteria.
        /// </summary>
        /// <param name="vendorID">The vendor ID, or null to not filter by vendor ID.</param>
        /// <param name="productID">The product ID, or null to not filter by product ID.</param>
        /// <param name="releaseNumberBcd">The device release number in binary-coded decimal, or null to not filter by device release number.</param>
        /// <param name="serialNumber">The serial number, or null to not filter by serial number.</param>
        /// <returns>The device, or null if none was found.</returns>
        public HidDevice GetHidDeviceOrNull(int? vendorID = null, int? productID = null, int? releaseNumberBcd = null, string serialNumber = null)
        {
            return GetHidDevices(vendorID, productID, releaseNumberBcd, serialNumber).FirstOrDefault();
        }

        public bool TryGetHidDevice(out HidDevice device, int? vendorID = null, int? productID = null, int? releaseNumberBcd = null, string serialNumber = null)
        {
            device = GetHidDeviceOrNull(vendorID, productID, releaseNumberBcd, serialNumber);
            return device != null;
        }

        /// <summary>
        /// Gets the connected serial device with the specific device path or filesystem name.
        /// </summary>
        /// <param name="portName">The device path or filesystem name.</param>
        /// <returns>The device, or null if none was found.</returns>
        public SerialDevice GetSerialDeviceOrNull(string portName)
        {
            return GetSerialDevices().Where(d =>
                {
                    if (d.DevicePath == portName) { return true; }

                    try
                    {
                        if (d.GetFileSystemName() == portName) { return true; }
                    }
                    catch
                    {

                    }

                    return false;
                }).FirstOrDefault();
        }

        public bool TryGetSerialDevice(out SerialDevice device, string portName)
        {
            device = GetSerialDeviceOrNull(portName);
            return device != null;
        }

        /// <summary>
        /// Raises the <see cref="Changed"/> event.
        /// </summary>
        public void RaiseChanged()
        {
            EventHandler<DeviceListChangedEventArgs> ev;

            ev = Changed;
            if (ev != null) { ev(this, new DeviceListChangedEventArgs()); }

            ev = DeviceListChanged;
            if (ev != null) { ev(this, new DeviceListChangedEventArgs()); }
        }

        /// <summary>
        /// <c>true</c> if drivers are presently being installed.
        /// </summary>
        public abstract bool AreDriversBeingInstalled
        {
            get;
        }

        /// <summary>
        /// The list of devices on this computer.
        /// </summary>
        public static DeviceList Local
        {
            get;
            private set;
        }
    }
}
