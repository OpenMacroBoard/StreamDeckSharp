using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HidSharp.Experimental
{
    public struct BleEvent
    {
        BleCharacteristic _characteristic;
        byte[] _value;

        public BleEvent(BleCharacteristic characteristic, byte[] value)
        {
            _characteristic = characteristic; _value = value;
        }

        public BleCharacteristic Characteristic
        {
            get { return _characteristic; }
        }

        public byte[] Value
        {
            get { return _value; }
        }
    }
}
