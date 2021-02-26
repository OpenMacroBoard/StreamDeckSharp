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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace HidSharp.Reports
{
    public delegate void ReportScanCallback(byte[] buffer, int bitOffset, DataItem dataItem, int indexOfDataItem);
    public delegate void ReportValueCallback(DataValue dataValue);

    /// <summary>
    /// Reads and writes HID reports.
    /// </summary>
    public class Report
    {
        bool _computed;
        int _computedLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="Report"/> class.
        /// </summary>
        public Report()
        {
            DataItems = new ReportDataItems(this);
        }

        public IEnumerable<uint> GetAllUsages()
        {
            return DataItems.SelectMany(item => item.Usages.GetAllValues());
        }

        /// <summary>
        /// Reads a HID report, calling back a provided function for each data item.
        /// </summary>
        /// <param name="buffer">The buffer containing the report.</param>
        /// <param name="offset">The offset to begin reading the report at.</param>
        /// <param name="callback">
        ///     This callback will be called for each data item.
        ///     Use this to read every value you need.
        /// </param>
        public void Read(byte[] buffer, int offset, ReportScanCallback callback)
        {
            Throw.If.Null(buffer).OutOfRange(buffer, offset, Length).Null(callback);
            if (buffer[offset] != ReportID) { throw new ArgumentException("Report ID not correctly set.", "buffer"); }
            int bitOffset = (offset + 1) * 8;

            var dataItems = DataItems;
            int dataItemCount = dataItems.Count;
            for (int indexOfDataItem = 0; indexOfDataItem < dataItemCount; indexOfDataItem ++)
            {
                var dataItem = dataItems[indexOfDataItem];
                callback(buffer, bitOffset, dataItem, indexOfDataItem);
                bitOffset += dataItem.TotalBits;
            }
        }

        public void Read(byte[] buffer, int offset, ReportValueCallback callback)
        {
            Read(buffer, offset, (readBuffer, bitOffset, dataItem, indexOfDataItem) =>
            {
                int elementCount = dataItem.ElementCount;
                for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
                {
                    DataValue value;
                    if (dataItem.TryReadValue(readBuffer, bitOffset, elementIndex, out value))
                    {
                        callback(value);
                    }
                }
            });
        }

        /// <summary>
        /// Writes a HID report, calling back a provided function for each data item.
        /// </summary>
        /// <param name="callback">
        ///     This callback will be called for each report data item.
        ///     Write to each data item to write a complete HID report.
        /// </param>
        public byte[] Write(ReportScanCallback callback)
        {
            byte[] buffer = new byte[Length];
            Write(buffer, 0, callback);
            return buffer;
        }

        public void Write(byte[] buffer, int offset, ReportScanCallback callback)
        {
            Throw.If.OutOfRange(buffer, offset, Length);
            buffer[offset] = ReportID; Array.Clear(buffer, offset + 1, Length - 1);
            Read(buffer, offset, callback);
        }

        internal void InvalidateBitCount()
        {
            _computed = false;
        }

        void ComputeLength()
        {
            if (_computed) { return; }

            {
                int bits = 0;
                foreach (DataItem dataItem in DataItems) { bits += dataItem.TotalBits; }
                _computedLength = (bits + 7) / 8 + 1;
            }

            _computed = true;
        }

        public DeviceItem DeviceItem
        {
            get;
            internal set;
        }

        public IList<DataItem> DataItems
        {
            get;
            private set;
        }

        /// <summary>
        /// The length of this particular report.
        /// The Report ID is included in this length.
        /// </summary>
        public int Length
        {
            get { ComputeLength(); return _computedLength; }
        }

        /// <summary>
        /// The Report ID.
        /// </summary>
        public byte ReportID
        {
            get;
            set;
        }

        public ReportType ReportType
        {
            get;
            set;
        }

        #region ReportDataItems
        sealed class ReportDataItems : Collection<DataItem>
        {
            Report _report;

            public ReportDataItems(Report report)
            {
                Debug.Assert(report != null);
                _report = report;
            }

            protected override void ClearItems()
            {
                foreach (var item in this) { item.Report = null; }
                base.ClearItems();
                _report.InvalidateBitCount();
            }

            protected override void InsertItem(int index, DataItem item)
            {
                Throw.If.Null(item).False(item.Report == null);
                item.Report = _report;
                base.InsertItem(index, item);
                _report.InvalidateBitCount();
            }

            protected override void RemoveItem(int index)
            {
                this[index].Report = null;
                base.RemoveItem(index);
                _report.InvalidateBitCount();
            }

            protected override void SetItem(int index, DataItem item)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
