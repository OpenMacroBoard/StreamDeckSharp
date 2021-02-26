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

using System;
using HidSharp.Experimental;

namespace HidSharp.Platform.Windows
{
    sealed class WinBleCharacteristic : Experimental.BleCharacteristic
    {
        internal NativeMethods.BTH_LE_GATT_CHARACTERISTIC NativeData;

        internal WinBleDescriptor[] _characteristicDescriptors;
        BleCharacteristicProperties _properties;

        public WinBleCharacteristic(NativeMethods.BTH_LE_GATT_CHARACTERISTIC nativeData)
        {
            NativeData = nativeData;

            _properties = (nativeData.IsBroadcastable != 0 ? BleCharacteristicProperties.Broadcast : 0)
                | (nativeData.IsReadable != 0 ? BleCharacteristicProperties.Read : 0)
                | (nativeData.IsWritableWithoutResponse != 0 ? BleCharacteristicProperties.WriteWithoutResponse : 0)
                | (nativeData.IsWritable != 0 ? BleCharacteristicProperties.Write : 0)
                | (nativeData.IsNotifiable != 0 ? BleCharacteristicProperties.Notify : 0)
                | (nativeData.IsIndicatable != 0 ? BleCharacteristicProperties.Indicate : 0)
                | (nativeData.IsSignedWritable != 0 ? BleCharacteristicProperties.SignedWrite : 0)
                | (nativeData.HasExtendedProperties != 0 ? BleCharacteristicProperties.ExtendedProperties : 0)
                ;
        }

        public override BleDescriptor[] GetDescriptors()
        {
            return (BleDescriptor[])_characteristicDescriptors.Clone();
        }

        public override BleUuid Uuid
        {
            get { return NativeData.CharacteristicUuid.ToGuid(); }
        }

        public override BleCharacteristicProperties Properties
        {
            get { return _properties; }
        }
    }
}
