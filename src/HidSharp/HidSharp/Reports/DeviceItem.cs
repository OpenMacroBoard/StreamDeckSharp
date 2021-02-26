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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace HidSharp.Reports
{
    public class DeviceItem : DescriptorCollectionItem
    {
        ReportCollectionItemReports _reports;

        public DeviceItem()
        {
            _reports = new ReportCollectionItemReports(this);
        }

        /// <summary>
        /// Creates a <see cref="HidSharp.Reports.Input.DeviceItemInputParser"/> appropriate for parsing reports for this device item.
        /// </summary>
        /// <returns>The new <see cref="HidSharp.Reports.Input.DeviceItemInputParser"/>.</returns>
        /// <remarks>
        /// Pair this with a <see cref="HidSharp.Reports.Input.HidDeviceInputReceiver"/> for the <see cref="ReportDescriptor"/>.
        /// </remarks>
        public Input.DeviceItemInputParser CreateDeviceItemInputParser()
        {
            return new Input.DeviceItemInputParser(this);
        }

        public IList<Report> Reports
        {
            get { return _reports; }
        }

        public IEnumerable<Report> InputReports
        {
            get { return Reports.Where(report => report.ReportType == ReportType.Input); }
        }

        public IEnumerable<Report> OutputReports
        {
            get { return Reports.Where(report => report.ReportType == ReportType.Output); }
        }

        public IEnumerable<Report> FeatureReports
        {
            get { return Reports.Where(report => report.ReportType == ReportType.Feature); }
        }

        #region ReportCollectionItemReports
        sealed class ReportCollectionItemReports : Collection<Report>
        {
            DeviceItem _item;

            public ReportCollectionItemReports(DeviceItem item)
            {
                Debug.Assert(item != null);
                _item = item;
            }

            protected override void ClearItems()
            {
                foreach (var item in this) { item.DeviceItem = null; }
                base.ClearItems();
            }

            protected override void InsertItem(int index, Report item)
            {
                Throw.If.Null(item).False(item.DeviceItem == null);
                item.DeviceItem = _item;
                base.InsertItem(index, item);
            }

            protected override void RemoveItem(int index)
            {
                this[index].DeviceItem = null;
                base.RemoveItem(index);
            }

            protected override void SetItem(int index, Report item)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
