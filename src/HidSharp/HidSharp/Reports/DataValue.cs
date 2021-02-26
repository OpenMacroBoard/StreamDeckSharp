#region License
/* Copyright 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Linq;

namespace HidSharp.Reports
{
    public struct DataValue
    {
        int _logicalValue;

        public int GetLogicalValue()
        {
            return _logicalValue;
        }

        public void SetLogicalValue(int logicalValue)
        {
            _logicalValue = logicalValue;
        }

        public double GetFractionalValue()
        {
            return GetScaledValue(0, 1);
        }

        public double GetScaledValue(double minimum, double maximum)
        {
            return DataConvert.CustomFromLogical(DataItem, GetLogicalValue(), minimum, maximum);
        }

        public double GetPhysicalValue()
        {
            return DataConvert.PhysicalFromLogical(DataItem, GetLogicalValue());
        }

        public DataItem DataItem
        {
            get;
            set;
        }

        public int DataIndex
        {
            get;
            set;
        }

        public bool IsNull
        {
            get { return !IsValid || (DataItem.IsVariable && DataConvert.IsLogicalOutOfRange(DataItem, GetLogicalValue())); }
        }

        public bool IsValid
        {
            get { return DataItem != null; }
        }

        public Report Report
        {
            get { return IsValid ? DataItem.Report : null; }
        }

        public IEnumerable<uint> Designators
        {
            get { return IsValid ? DataItem.Designators.GetValuesFromIndex(DataIndex) : Enumerable.Empty<uint>(); }
        }

        public IEnumerable<uint> Strings
        {
            get { return IsValid ? DataItem.Strings.GetValuesFromIndex(DataIndex) : Enumerable.Empty<uint>(); }
        }

        public IEnumerable<uint> Usages
        {
            get { return IsValid ? DataItem.Usages.GetValuesFromIndex(DataIndex) : Enumerable.Empty<uint>(); }
        }
    }
}
