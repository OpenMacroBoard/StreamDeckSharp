#region License
/* Copyright 2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

using System.Collections.Generic;
using HidSharp.Experimental;

namespace HidSharp.Platform.Windows
{
    sealed class WinBleService : BleService
    {
        internal NativeMethods.BTH_LE_GATT_SERVICE NativeData;

        WinBleCharacteristic[] _characteristics;
        WinBleDevice _device;
        object _syncObject;

        public WinBleService(WinBleDevice device, NativeMethods.BTH_LE_GATT_SERVICE nativeData)
        {
            _device = device; NativeData = nativeData; _syncObject = new object();
        }

        public override BleCharacteristic[] GetCharacteristics()
        {
            lock (_syncObject)
            {
                if (_characteristics == null)
                {
                    if (!_device.TryOpenToGetInfo(handle =>
                        {
                            var nativeCharacteristics = NativeMethods.BluetoothGATTGetCharacteristics(handle, ref NativeData);
                            if (nativeCharacteristics == null) { return false; }

                            var characteristics = new List<WinBleCharacteristic>();
                            foreach (var nativeCharacteristic in nativeCharacteristics)
                            {
                                var characteristic = new WinBleCharacteristic(nativeCharacteristic);
                                characteristics.Add(characteristic);

                                var nativeDescriptors = NativeMethods.BluetoothGATTGetDescriptors(handle, ref characteristic.NativeData);
                                if (nativeDescriptors == null) { return false; }

                                var descriptors = new List<WinBleDescriptor>();
                                foreach (var nativeDescriptor in nativeDescriptors)
                                {
                                    var descriptor = new WinBleDescriptor(nativeDescriptor);
                                    descriptors.Add(descriptor);
                                }

                                characteristic._characteristicDescriptors = descriptors.ToArray();
                            }

                            _characteristics = characteristics.ToArray();
                            return true;
                        }))
                    {
                        throw DeviceException.CreateIOException(_device, "BLE service information could not be retrieved.");
                    }
                }
            }

            return (BleCharacteristic[])_characteristics.Clone();
        }

        public override BleDevice Device
        {
            get { return _device; }
        }

        public override BleUuid Uuid
        {
            get { return NativeData.ServiceUuid.ToGuid(); }
        }
    }
}
