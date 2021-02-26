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
    public abstract class BleService
    {
        public override string ToString()
        {
            return Uuid.ToString();
        }

        public abstract BleCharacteristic[] GetCharacteristics();

        public BleCharacteristic GetCharacteristicOrNull(BleUuid uuid)
        {
            BleCharacteristic characteristic;
            return TryGetCharacteristic(uuid, out characteristic) ? characteristic : null;
        }

        public virtual bool HasCharacteristic(BleUuid uuid)
        {
            BleCharacteristic characteristic;
            return TryGetCharacteristic(uuid, out characteristic);
        }

        public virtual bool TryGetCharacteristic(BleUuid uuid, out BleCharacteristic characteristic)
        {
            foreach (var c in GetCharacteristics())
            {
                if (c.Uuid == uuid) { characteristic = c; return true; }
            }

            characteristic = null; return false;
        }

        public abstract BleDevice Device
        {
            get;
        }

        public abstract BleUuid Uuid
        {
            get;
        }
    }
}
