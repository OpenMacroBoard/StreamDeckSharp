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
    public static class DataConvert
    {
        public static int LogicalFromPhysical(DataItem item, double physicalValue)
        {
            Throw.If.Null(item);
            if (item.IsArray) { return (physicalValue > 0) ? 1 : 0; }
            return LogicalFromCustom(item, physicalValue, item.PhysicalMinimum, item.PhysicalMaximum);
        }

        public static int LogicalFromCustom(DataItem item, double physicalValue, double minimum, double maximum)
        {
            return Math.Max(item.LogicalMinimum, Math.Min(item.LogicalMaximum, (int)Math.Round((physicalValue - minimum) * item.LogicalRange / (maximum - minimum))));
        }

        public static int LogicalFromRaw(DataItem item, uint value)
        {
            Throw.If.Null(item);
            uint signBit = 1u << (item.ElementBits - 1), mask = signBit - 1;
            return (value & signBit) != 0 ? (int)(value | ~mask) : (int)value;
        }

        public static int DataIndexFromLogical(DataItem item, int logicalValue)
        {
            Throw.If.Null(item);
            if (!item.IsArray) { throw new ArgumentException("Data item is not an array.", "item"); }
            return IsLogicalOutOfRange(item, logicalValue) ? -1 : logicalValue - item.LogicalMinimum;
        }

        public static double PhysicalFromLogical(DataItem item, int logicalValue)
        {
            Throw.If.Null(item);
            if (item.IsArray) { return (logicalValue > 0) ? 1 : 0; }
            return CustomFromLogical(item, logicalValue, item.PhysicalMinimum, item.PhysicalRange);
        }

        public static double CustomFromLogical(DataItem item, int logicalValue, double minimum, double maximum)
        {
            if (IsLogicalOutOfRange(item, logicalValue)) { return double.NaN; }
            return minimum + (logicalValue - item.LogicalMinimum) * (maximum - minimum) / item.LogicalRange;
        }

        public static uint RawFromLogical(DataItem item, int value)
        {
            Throw.If.Null(item);
            uint usValue = (uint)value;
            uint signBit = 1u << (item.ElementBits - 1), mask = signBit - 1;
            return (usValue & mask) | (value < 0 ? signBit : 0);
        }

        public static bool IsLogicalOutOfRange(DataItem item, int logicalValue)
        {
            Throw.If.Null(item);
            return item.IsLogicalSigned
                ? (logicalValue < item.LogicalMinimum || logicalValue > item.LogicalMaximum)
                : ((uint)logicalValue < (uint)item.LogicalMinimum || (uint)logicalValue > (uint)item.LogicalMaximum)
                ;
        }
    }
}
