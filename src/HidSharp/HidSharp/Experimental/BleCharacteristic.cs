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

namespace HidSharp.Experimental
{
    public abstract class BleCharacteristic
    {
        public override string ToString()
        {
            return string.Format("{0} (properties: {1})", Uuid, Properties);
        }

        public abstract BleDescriptor[] GetDescriptors();

        public bool HasDescriptor(BleUuid uuid)
        {
            BleDescriptor descriptor;
            return TryGetDescriptor(uuid, out descriptor);
        }

        public BleDescriptor GetDescriptorOrNull(BleUuid uuid)
        {
            BleDescriptor descriptor;
            return TryGetDescriptor(uuid, out descriptor) ? descriptor : null;
        }

        public virtual bool TryGetDescriptor(BleUuid uuid, out BleDescriptor descriptor)
        {
            foreach (var d in GetDescriptors())
            {
                if (d.Uuid == uuid) { descriptor = d; return true; }
            }

            descriptor = null; return false;
        }

        public abstract BleUuid Uuid
        {
            get;
        }

        public abstract BleCharacteristicProperties Properties
        {
            get;
        }

        public bool IsReadable
        {
            get { return (Properties & BleCharacteristicProperties.Read) != 0; }
        }

        public bool IsWritable
        {
            get { return (Properties & BleCharacteristicProperties.Write) != 0; }
        }

        public bool IsWritableWithoutResponse
        {
            get { return (Properties & BleCharacteristicProperties.WriteWithoutResponse) != 0; }
        }

        public bool IsNotifiable
        {
            get { return (Properties & BleCharacteristicProperties.Notify) != 0; }
        }

        public bool IsIndicatable
        {
            get { return (Properties & BleCharacteristicProperties.Indicate) != 0; }
        }
    }
}
