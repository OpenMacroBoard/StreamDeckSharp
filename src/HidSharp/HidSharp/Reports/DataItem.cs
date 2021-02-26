#region License
/* Copyright 2011, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Reports
{
    public class DataItem : DescriptorItem
    {
        int _elementBits;
        int _elementSize;

        int _rawPhysicalMinimum;
        int _rawPhysicalMaximum;
        int _unitExponent;

        // derived values
        int _totalBits;
        double _unitMultiplier;
        int _rawPhysicalRange;
        double _physicalMinimum;
        double _physicalMaximum;
        double _physicalRange;

        public DataItem()
        {
            UnitExponent = 0;
        }

        public int ReadLogical(byte[] buffer, int bitOffset, int elementIndex)
        {
            uint rawValue = ReadRaw(buffer, bitOffset, elementIndex);
            return IsLogicalSigned ? DataConvert.LogicalFromRaw(this, rawValue) : (int)rawValue;
        }

        public uint ReadRaw(byte[] buffer, int bitOffset, int elementIndex)
        {
            Throw.If.Null(buffer).OutOfRange(ElementCount, elementIndex, 1);

            uint value = 0; int totalBits = Math.Min(ElementBits, 32);
            bitOffset += elementIndex * ElementBits;

            for (int i = 0; i < totalBits; i++, bitOffset ++)
            {
                int byteStart = bitOffset >> 3; byte bitStart = (byte)(1u << (bitOffset & 7));
                value |= (buffer[byteStart] & bitStart) != 0 ? (1u << i) : 0;
            }

            return value;
        }

        public bool TryReadValue(byte[] buffer, int bitOffset, int elementIndex, out DataValue value)
        {
            value = new DataValue();

            int logicalValue = ReadLogical(buffer, bitOffset, elementIndex);
            if (IsArray)
            {
                int dataIndex = DataConvert.DataIndexFromLogical(this, logicalValue);
                if (dataIndex < 0) { return false; }

                value.DataItem = this;
                value.DataIndex = logicalValue;
                value.SetLogicalValue(1);
            }
            else
            {
                value.DataItem = this;
                value.DataIndex = elementIndex;
                value.SetLogicalValue(logicalValue);
            }

            return true;
        }

        public void WriteLogical(byte[] buffer, int bitOffset, int elementIndex, int logicalValue)
        {
            WriteRaw(buffer, bitOffset, elementIndex, IsLogicalSigned ? DataConvert.RawFromLogical(this, logicalValue) : (uint)logicalValue);
        }

        public void WriteRaw(byte[] buffer, int bitOffset, int elementIndex, uint rawValue)
        {
            Throw.If.Null(buffer).OutOfRange(ElementCount, elementIndex, 1);

            int totalBits = Math.Min(ElementBits, 32);
            bitOffset += elementIndex * ElementBits;

            for (int i = 0; i < totalBits; i++, bitOffset++)
            {
                int byteStart = bitOffset >> 3; uint bitStart = 1u << (bitOffset & 7);
                if ((rawValue & (1 << i)) != 0) { buffer[byteStart] |= (byte)bitStart; } else { buffer[byteStart] &= (byte)(~bitStart); }
            }
        }

        void InvalidateBitCount()
        {
            _totalBits = ElementCount * ElementBits;
            if (Report != null) { Report.InvalidateBitCount(); }
        }

        void InvalidatePhysicalRange()
        {
            _unitMultiplier = Math.Pow(10, UnitExponent);
            _rawPhysicalRange = RawPhysicalMaximum - RawPhysicalMinimum;
            _physicalMinimum = RawPhysicalMinimum * UnitMultiplier;
            _physicalMaximum = RawPhysicalMaximum * UnitMultiplier;
            _physicalRange = PhysicalMaximum - PhysicalMinimum;
        }

        public int TotalBits
        {
            get { return _totalBits; }
        }

        public int ElementCount
        {
            get { return _elementBits; }
            set
            {
                Throw.If.Negative(value, "value");
                _elementBits = value; InvalidateBitCount();
            }
        }

        public int ElementBits
        {
            get { return _elementSize; }
            set
            {
                Throw.If.Negative(value, "value");
                _elementSize = value; InvalidateBitCount();
            }
        }

        public DataItemFlags Flags
        {
            get;
            set;
        }

        public bool HasNullState
        {
            get { return 0 != (Flags & DataItemFlags.NullState); }
        }

        public bool HasPreferredState
        {
            get { return 0 == (Flags & DataItemFlags.NoPreferred); }
        }

        public bool IsArray
        {
            get { return !IsVariable; }
        }

        public bool IsVariable
        {
            get { return 0 != (Flags & DataItemFlags.Variable); }
        }

        public bool IsBoolean
        {
            get { return ElementBits == 1 || IsArray; }
        }

        public bool IsConstant
        {
            get { return 0 != (Flags & DataItemFlags.Constant); }
        }

        public bool IsAbsolute
        {
            get { return !IsRelative; }
        }

        public bool IsRelative
        {
            get { return 0 != (Flags & DataItemFlags.Relative); }
        }

        public ExpectedUsageType ExpectedUsageType
        {
            get
            {
                if (!IsConstant)
                {
                    if (IsBoolean)
                    {
                        if (IsAbsolute && HasPreferredState)
                        {
                            return ExpectedUsageType.PushButton;
                        }
                        else if (IsAbsolute && !HasPreferredState)
                        {
                            return ExpectedUsageType.ToggleButton;
                        }
                        else if (IsRelative && HasPreferredState)
                        {
                            return ExpectedUsageType.OneShot;
                        }
                    }
                    else if (IsVariable)
                    {
                        if (IsRelative && HasPreferredState && ElementBits >= 2 && -LogicalMinimum == LogicalMaximum && LogicalMaximum >= 1)
                        {
                            if (LogicalMaximum == 1)
                            {
                                return ExpectedUsageType.UpDown;
                            }
                            else
                            {
                                //return ExpectedUsageType.JogWheel;
                            }
                        }
                    }
                }

                return 0;
            }
        }

        public bool IsLogicalSigned
        {
            get;
            set;
        }

        public int LogicalMinimum
        {
            get;
            set;
        }

        public int LogicalMaximum
        {
            get;
            set;
        }

        public int LogicalRange
        {
            get { return LogicalMaximum - LogicalMinimum; }
        }

        public double PhysicalMinimum
        {
            get { return _physicalMinimum; }
        }

        public double PhysicalMaximum
        {
            get { return _physicalMaximum; }
        }

        public double PhysicalRange
        {
            get { return _physicalRange; }
        }

        public int RawPhysicalMinimum
        {
            get { return _rawPhysicalMinimum; }
            set { _rawPhysicalMinimum = value; InvalidatePhysicalRange(); }
        }

        public int RawPhysicalMaximum
        {
            get { return _rawPhysicalMaximum; }
            set { _rawPhysicalMaximum = value; InvalidatePhysicalRange(); }
        }

        public int RawPhysicalRange
        {
            get { return _rawPhysicalRange; }
        }

        public Report Report
        {
            get;
            internal set;
        }

        public Units.Unit Unit
        {
            get;
            set;
        }

        public int UnitExponent
        {
            get { return _unitExponent; }
            set { _unitExponent = value; InvalidatePhysicalRange(); }
        }

        double UnitMultiplier
        {
            get { return _unitMultiplier; }
        }
    }
}
