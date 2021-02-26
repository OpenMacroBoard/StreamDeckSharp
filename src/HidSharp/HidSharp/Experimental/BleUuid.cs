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
using System.Globalization;

namespace HidSharp.Experimental
{
    public struct BleUuid : IEquatable<BleUuid>
    {
        Guid _guid;

        public BleUuid(int uuid) : this()
        {
            Initialize(uuid);
        }

        public BleUuid(Guid guid) : this()
        {
            Initialize(guid);
        }

        public BleUuid(string uuid) : this()
        {
            Initialize(uuid);
        }

        void Initialize(int uuid)
        {
            _guid = new Guid(uuid, 0x0000, 0x1000, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB);
        }

        void Initialize(Guid guid)
        {
            _guid = guid;
        }

        void Initialize(string guid)
        {
            uint shortUuid;
            if (uint.TryParse(guid, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out shortUuid))
            {
                Initialize((int)shortUuid);
            }
            else
            {
                Initialize(new Guid(guid));
            }
        }

        public override bool Equals(object other)
        {
            return other is BleUuid && Equals((BleUuid)other);
        }

        public bool Equals(BleUuid other)
        {
            return _guid.Equals(other._guid);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public static implicit operator BleUuid(Guid guid)
        {
            return new BleUuid(guid);
        }

        public static implicit operator Guid(BleUuid uuid)
        {
            return uuid.ToGuid();
        }

        public int ToShortUuid()
        {
            if (!IsShortUuid) { throw new InvalidOperationException(); }

            byte[] bytes = _guid.ToByteArray();
            return (ushort)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
        }

        static void SwapNetworkOrder(byte[] guid)
        {
            byte temp;
            temp = guid[0]; guid[0] = guid[3]; guid[3] = temp;
            temp = guid[1]; guid[1] = guid[2]; guid[2] = temp;
            temp = guid[4]; guid[4] = guid[5]; guid[5] = temp;
            temp = guid[6]; guid[6] = guid[7]; guid[7] = temp;
        }

        public byte[] ToByteArray()
        {
            var guid = _guid.ToByteArray();
            SwapNetworkOrder(guid);
            return guid;
        }

        public Guid ToGuid()
        {
            return _guid;
        }

        public override string ToString()
        {
            return IsShortUuid ? ToShortUuid().ToString("X", CultureInfo.InvariantCulture) : ToGuid().ToString("D", CultureInfo.InvariantCulture);
        }

        public bool IsShortUuid
        {
            get
            {
                byte[] bytes = _guid.ToByteArray();
                return bytes[4] == 0x00 && bytes[5] == 0x00
                    && bytes[6] == 0x00 && bytes[7] == 0x10
                    && bytes[8] == 0x80 && bytes[9] == 0x00 && bytes[10] == 0x00 && bytes[11] == 0x80 && bytes[12] == 0x5F && bytes[13] == 0x9B && bytes[14] == 0x34 && bytes[15] == 0xFB;
            }
        }

        public static bool operator ==(BleUuid lhs, BleUuid rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(BleUuid lhs, BleUuid rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
