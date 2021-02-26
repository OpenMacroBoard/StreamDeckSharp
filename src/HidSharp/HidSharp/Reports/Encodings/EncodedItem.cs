#region License
/* Copyright 2011, 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Reports.Encodings
{
    public class EncodedItem
    {
        public EncodedItem()
        {
            Data = new List<byte>();
        }

        public override string ToString()
        {
            switch (ItemType)
            {
                case ItemType.Global: return string.Format("{0} {1}", TagForGlobal, DataValue);
                case ItemType.Local: return string.Format("{0} {1}", TagForLocal, DataValue);
                case ItemType.Main: return string.Format("{0} {1}", TagForMain, DataValue);
                default: return Tag.ToString();
            }
        }

        public void Reset()
        {
            Data.Clear(); Tag = 0; ItemType = ItemType.Main;
        }

        byte DataAt(int index)
        {
            return index >= 0 && index < Data.Count ? Data[index] : (byte)0;
        }

        static byte GetByte(IList<byte> buffer, ref int offset, ref int count)
        {
            if (count <= 0) { return 0; } else { count--; }
            return offset >= 0 && offset < buffer.Count ? buffer[offset++] : (byte)0;
        }

        public int Decode(IList<byte> buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            Reset(); int startCount = count;
            byte header = GetByte(buffer, ref offset, ref count);

            int size = header & 0x3; if (size == 3) { size = 4; }
            ItemType = (ItemType)((header >> 2) & 0x3); Tag = (byte)(header >> 4);
            for (int i = 0; i < size; i++) { Data.Add(GetByte(buffer, ref offset, ref count)); }
            return startCount - count;
        }

        public static IEnumerable<EncodedItem> DecodeItems(IList<byte> buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            while (count > 0)
            {
                EncodedItem item = new EncodedItem();
                int bytes = item.Decode(buffer, offset, count);
                offset += bytes; count -= bytes;
                yield return item;
            }
        }

        /*
        // TODO: What was I intending to use this for?
        public static IEnumerable<EncodedItem> DecodeHIDDT(IList<byte> buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            while (count > 34)
            {
                EncodedItem item = new EncodedItem();
                int bytes = item.Decode(buffer, offset + 34, count - 34);
                offset += 10; count -= 10;
                yield return item;
            }
        }
        */

        public void Encode(IList<byte> buffer)
        {
            Throw.If.Null(buffer, "buffer");

            if (buffer == null) { throw new ArgumentNullException("buffer"); }
            if (!IsShortTag) { return; } // TODO

            int size = DataSize;
            buffer.Add((byte)((size == 4 ? (byte)3 : size) | (byte)ItemType << 2 | Tag << 4));
            foreach (byte @byte in Data) { buffer.Add(@byte); }
        }

        public static void EncodeItems(IEnumerable<EncodedItem> items, IList<byte> buffer)
        {
            Throw.If.Null(buffer, "buffer").Null(items, "items");
            foreach (EncodedItem item in items) { item.Encode(buffer); }
        }

        public IList<byte> Data
        {
            get;
            private set;
        }

        public int DataSize
        {
            get { return IsShortTag ? Data.Count : 0; }
        }

        public uint DataValue
        {
            get
            {
                if (!IsShortTag) { return 0; }
                return (uint)(DataAt(0) | DataAt(1) << 8 | DataAt(2) << 16 | DataAt(3) << 24);
            }

            set
            {
                Data.Clear();
                Data.Add((byte)value);
                if (value > 0xff) { Data.Add((byte)(value >> 8)); }
                if (value > 0xffff) { Data.Add((byte)(value >> 16)); Data.Add((byte)(value >> 24)); }
            }
        }

        public int DataValueSigned
        {
            get
            {
                if (!IsShortTag) { return 0; }
                return Data.Count == 4 ? (int)DataValue :
                    Data.Count == 2 ? (short)DataValue :
                    Data.Count == 1 ? (sbyte)DataValue : (sbyte)0;
            }

            set
            {
                if (value == 0)
                    { DataValue = (uint)value; }
                else if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                    { DataValue = (uint)(sbyte)value; if (value < 0) { Data.Add(0); } }
                else if (value >= short.MinValue && value <= short.MaxValue)
                    { DataValue = (uint)(short)value; if (value < 0) { Data.Add(0); Data.Add(0); } }
                else
                    { DataValue = (uint)value; }
            }
        }

        public byte Tag
        {
            get;
            set;
        }

        public GlobalItemTag TagForGlobal
        {
            get { return (GlobalItemTag)Tag; }
            set { Tag = (byte)value; }
        }

        public LocalItemTag TagForLocal
        {
            get { return (LocalItemTag)Tag; }
            set { Tag = (byte)value; }
        }

        public MainItemTag TagForMain
        {
            get { return (MainItemTag)Tag; }
            set { Tag = (byte)value; }
        }

        public ItemType ItemType
        {
            get;
            set;
        }

        public bool IsShortTag
        {
            get { return !IsLongTag && (Data.Count == 0 || Data.Count == 1 || Data.Count == 2 || Data.Count == 4); }
        }

        public bool IsLongTag
        {
            get { return Tag == 15 && ItemType == ItemType.Reserved && Data.Count >= 2; }
        }
    }
}
