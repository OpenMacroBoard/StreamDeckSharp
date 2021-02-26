#region License
/* Copyright 2011, 2013, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Linq;
using HidSharp.Reports.Encodings;

namespace HidSharp.Reports
{
    /// <summary>
    /// Parses HID report descriptors.
    /// </summary>
    public class ReportDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportDescriptor"/> class.
        /// </summary>
        ReportDescriptor()
        {
            State = new ReportDescriptorParseState();
            StartParsing();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportDescriptor"/> class, and parses a raw HID report descriptor.
        /// </summary>
        /// <param name="buffer">The buffer containing the report descriptor.</param>
        public ReportDescriptor(byte[] buffer)
            : this()
        {
            StartParsing();
            ParseRawReportDescriptor(buffer);
            FinishParsing();
        }

        /// <summary>
        /// Creates an <see cref="HidSharp.Reports.Input.HidDeviceInputReceiver"/> appropriate
        /// for receiving reports from this device.
        /// </summary>
        /// <returns>The new <see cref="HidSharp.Reports.Input.HidDeviceInputReceiver"/>.</returns>
        /// <remarks>
        /// Pair this with a <see cref="HidSharp.Reports.Input.DeviceItemInputParser"/> for your chosen <see cref="DeviceItem"/>.
        /// </remarks>
        public Input.HidDeviceInputReceiver CreateHidDeviceInputReceiver()
        {
            return new Input.HidDeviceInputReceiver(this);
        }

        /// <summary>
        /// Resets the parser to its initial state.
        /// </summary>
        void StartParsing()
        {
            Reports = new List<Report>();
            ReportsUseID = false;
            State.Reset();
        }

        /// <summary>
        /// Updates derived data.
        /// </summary>
        void FinishParsing()
        {
            Reports = Array.AsReadOnly(Reports.ToArray());
            MaxInputReportLength = GetMaxLengthOfReports(InputReports);
            MaxOutputReportLength = GetMaxLengthOfReports(OutputReports);
            MaxFeatureReportLength = GetMaxLengthOfReports(FeatureReports);
            DeviceItems = Array.AsReadOnly(RootItem.ChildItems.OfType<DeviceItem>().ToArray());
        }

        public Report GetReport(ReportType type, byte id)
        {
            Report report;
            if (!TryGetReport(type, id, out report)) { throw new ArgumentException("Report not found."); }
            return report;
        }

        public bool TryGetReport(ReportType type, byte id, out Report report)
        {
            for (int i = 0; i < Reports.Count; i++)
            {
                report = Reports[i];
                if (report.ReportType == type && report.ReportID == id) { return true; }
            }

            report = null; return false;
        }

        static int GetMaxLengthOfReports(IEnumerable<Report> reports)
        {
            int length = 0;
            foreach (Report report in reports) { length = Math.Max(length, report.Length); }
            return length;
        }

        /// <summary>
        /// Parses a raw HID report descriptor.
        /// </summary>
        /// <param name="buffer">The buffer containing the report descriptor.</param>
        void ParseRawReportDescriptor(byte[] buffer)
        {
            Throw.If.Null(buffer, "buffer");

            var items = EncodedItem.DecodeItems(buffer, 0, buffer.Length);
            ParseEncodedItems(items);
        }

        /// <summary>
        /// Parses all of the <see cref="EncodedItem"/> elements in a report descriptor.
        /// </summary>
        /// <param name="items">The items to parse.</param>
        void ParseEncodedItems(IEnumerable<EncodedItem> items)
        {
            Throw.If.Null(items, "items");
            foreach (EncodedItem item in items) { ParseEncodedItem(item); }
        }

        /// <summary>
        /// Parses a single <see cref="EncodedItem"/>.
        /// Call this repeatedly for every item to completely decode a report descriptor.
        /// </summary>
        /// <param name="item">The item to parse.</param>
        void ParseEncodedItem(EncodedItem item)
        {
            Throw.If.Null(item, "item");
            uint value = item.DataValue;

            switch (item.ItemType)
            {
                case ItemType.Main:
                    ParseMain(item.TagForMain, value);
                    State.LocalItemState.Clear();
                    break;

                case ItemType.Local:
                    switch (item.TagForLocal)
                    {
                        case LocalItemTag.Usage:
                        case LocalItemTag.UsageMinimum:
                        case LocalItemTag.UsageMaximum:
                            if (value <= 0xffff) { value |= State.GetGlobalItemValue(GlobalItemTag.UsagePage) << 16; }
                            break;
                    }
                    State.LocalItemState.Add(new KeyValuePair<LocalItemTag, uint>(item.TagForLocal, value));
                    break;

                case ItemType.Global:
                    switch (item.TagForGlobal)
                    {
                        case GlobalItemTag.Push:
                            State.GlobalItemStateStack.Add(new Dictionary<GlobalItemTag, EncodedItem>(State.GlobalItemState));
                            break;

                        case GlobalItemTag.Pop:
                            State.GlobalItemStateStack.RemoveAt(State.GlobalItemState.Count - 1);
                            break;

                        default:
                            switch (item.TagForGlobal)
                            {
                                case GlobalItemTag.ReportID:
                                    ReportsUseID = true; break;
                            }

                            State.GlobalItemState[item.TagForGlobal] = item;
                            break;
                    }
                    break;
            }
        }

        void ParseMain(MainItemTag tag, uint value)
        {
            switch (tag)
            {
                case MainItemTag.Collection:
                    ParseMainCollection(value); break;

                case MainItemTag.EndCollection:
                    ParseMainCollectionEnd(); break;

                case MainItemTag.Input:
                case MainItemTag.Output:
                case MainItemTag.Feature:
                    ParseMainData(tag, value); break;
            }
        }

        void ParseMainCollection(uint value)
        {
            DescriptorCollectionItem collection = State.CurrentCollectionItem != State.RootItem ? new DescriptorCollectionItem() : new DeviceItem();
            collection.CollectionType = (CollectionType)value;
            State.CurrentCollectionItem.ChildItems.Add(collection);
            State.CurrentCollectionItem = collection;
            ParseMainIndexes(collection);
        }

        void ParseMainCollectionEnd()
        {
            State.CurrentCollectionItem = State.CurrentCollectionItem.ParentItem;
        }

        static void AddIndex(List<KeyValuePair<int, uint>> list, int action, uint value)
        {
            list.Add(new KeyValuePair<int, uint>(action, value));
        }

        static void UpdateIndexMinimum(ref Indexes index, uint value)
        {
            if (!(index is IndexRange)) { index = new IndexRange(); }
            ((IndexRange)index).Minimum = value;
        }

        static void UpdateIndexMaximum(ref Indexes index, uint value)
        {
            if (!(index is IndexRange)) { index = new IndexRange(); }
            ((IndexRange)index).Maximum = value;
        }

        static void UpdateIndexList(List<uint> values, int delimiter,
                                    ref Indexes index, uint value)
        {
            values.Add(value);
            UpdateIndexListCommit(values, delimiter, ref index);
        }

        static void UpdateIndexListCommit(List<uint> values, int delimiter,
                                          ref Indexes index)
        {
            if (delimiter != 0 || values.Count == 0) { return; }
            if (!(index is IndexList)) { index = new IndexList(); }
            ((IndexList)index).Indices.Add(new List<uint>(values));
            values.Clear();
        }

        void ParseMainIndexes(DescriptorItem item)
        {
            int delimiter = 0;
            List<uint> designatorValues = new List<uint>(); Indexes designator = Indexes.Unset;
            List<uint> stringValues = new List<uint>(); Indexes @string = Indexes.Unset;
            List<uint> usageValues = new List<uint>(); Indexes usage = Indexes.Unset;

            foreach (KeyValuePair<LocalItemTag, uint> kvp in State.LocalItemState)
            {
                switch (kvp.Key)
                {
                    case LocalItemTag.DesignatorMinimum: UpdateIndexMinimum(ref designator, kvp.Value); break;
                    case LocalItemTag.StringMinimum: UpdateIndexMinimum(ref @string, kvp.Value); break;
                    case LocalItemTag.UsageMinimum: UpdateIndexMinimum(ref usage, kvp.Value); break;

                    case LocalItemTag.DesignatorMaximum: UpdateIndexMaximum(ref designator, kvp.Value); break;
                    case LocalItemTag.StringMaximum: UpdateIndexMaximum(ref @string, kvp.Value); break;
                    case LocalItemTag.UsageMaximum: UpdateIndexMaximum(ref usage, kvp.Value); break;

                    case LocalItemTag.DesignatorIndex: UpdateIndexList(designatorValues, delimiter, ref designator, kvp.Value); break;
                    case LocalItemTag.StringIndex: UpdateIndexList(stringValues, delimiter, ref @string, kvp.Value); break;
                    case LocalItemTag.Usage: UpdateIndexList(usageValues, delimiter, ref usage, kvp.Value); break;

                    case LocalItemTag.Delimiter:
                        if (kvp.Value == 1)
                        {
                            if (delimiter++ == 0)
                            {
                                designatorValues.Clear();
                                stringValues.Clear();
                                usageValues.Clear();
                            }
                        }
                        else if (kvp.Value == 0)
                        {
                            delimiter--;
                            UpdateIndexListCommit(designatorValues, delimiter, ref designator);
                            UpdateIndexListCommit(stringValues, delimiter, ref @string);
                            UpdateIndexListCommit(usageValues, delimiter, ref usage);
                        }
                        break;
                }
            }

            item.Designators = designator;
            item.Strings = @string;
            item.Usages = usage;
        }

        void ParseMainData(MainItemTag tag, uint value)
        {
            DataItem dataItem = new DataItem();
            dataItem.Flags = (DataItemFlags)value;
            dataItem.ParentItem = State.CurrentCollectionItem;
            dataItem.ElementCount = (int)State.GetGlobalItemValue(GlobalItemTag.ReportCount);
            dataItem.ElementBits = (int)State.GetGlobalItemValue(GlobalItemTag.ReportSize);
            dataItem.Unit = new Units.Unit(State.GetGlobalItemValue(GlobalItemTag.Unit));
            dataItem.UnitExponent = Units.Unit.DecodeExponent(State.GetGlobalItemValue(GlobalItemTag.UnitExponent));

            EncodedItem logicalMinItem = State.GetGlobalItem(GlobalItemTag.LogicalMinimum);
            EncodedItem logicalMaxItem = State.GetGlobalItem(GlobalItemTag.LogicalMaximum);
            dataItem.IsLogicalSigned = !dataItem.IsArray && ((logicalMinItem != null ? logicalMinItem.DataValue : 0) > (logicalMaxItem != null ? logicalMaxItem.DataValue : 0));
            int logicalMinimum = logicalMinItem == null ? 0 : dataItem.IsLogicalSigned ? logicalMinItem.DataValueSigned : (int)logicalMinItem.DataValue;
            int logicalMaximum = logicalMaxItem == null ? 0 : dataItem.IsLogicalSigned ? logicalMaxItem.DataValueSigned : (int)logicalMaxItem.DataValue;

            EncodedItem physicalMinItem = State.GetGlobalItem(GlobalItemTag.PhysicalMinimum);
            EncodedItem physicalMaxItem = State.GetGlobalItem(GlobalItemTag.PhysicalMaximum);
            bool isPhysicalSigned = !dataItem.IsArray && ((physicalMinItem != null ? physicalMinItem.DataValue : 0) > (physicalMaxItem != null ? physicalMaxItem.DataValue : 0));
            int physicalMinimum = physicalMinItem == null ? 0 : isPhysicalSigned ? physicalMinItem.DataValueSigned : (int)physicalMinItem.DataValue;
            int physicalMaximum = physicalMaxItem == null ? 0 : isPhysicalSigned ? physicalMaxItem.DataValueSigned : (int)physicalMaxItem.DataValue;

            if (physicalMinimum == 0 && physicalMaximum == 0)
            {
                physicalMinimum = logicalMinimum; physicalMaximum = logicalMaximum;
            }

            dataItem.LogicalMinimum = logicalMinimum; dataItem.LogicalMaximum = logicalMaximum;
            dataItem.RawPhysicalMinimum = physicalMinimum; dataItem.RawPhysicalMaximum = physicalMaximum;

            Report report;
            ReportType reportType
                = tag == MainItemTag.Output ? ReportType.Output
                : tag == MainItemTag.Feature ? ReportType.Feature
                : ReportType.Input;
            uint reportID = State.GetGlobalItemValue(GlobalItemTag.ReportID);
            if (!TryGetReport(reportType, (byte)reportID, out report))
            {
                report = new Report() { ReportID = (byte)reportID, ReportType = reportType };
                Reports.Add(report);

                var collection = State.CurrentCollectionItem;
                while (collection != null && !(collection is DeviceItem)) { collection = collection.ParentItem; }
                if (collection is DeviceItem) { ((DeviceItem)collection).Reports.Add(report); }
            }
            report.DataItems.Add(dataItem);

            ParseMainIndexes(dataItem);
        }

        /// <summary>
        /// The maximum input report length.
        /// The Report ID is included in this length.
        /// </summary>
        public int MaxInputReportLength
        {
            get;
            private set;
        }

        /// <summary>
        /// The maximum output report length.
        /// The Report ID is included in this length.
        /// </summary>
        public int MaxOutputReportLength
        {
            get;
            private set;
        }

        /// <summary>
        /// The maximum feature report length.
        /// The Report ID is included in this length.
        /// </summary>
        public int MaxFeatureReportLength
        {
            get;
            private set;
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

        public IList<Report> Reports
        {
            get;
            private set;
        }

        /// <summary>
        /// True if the device sends Report IDs.
        /// </summary>
        public bool ReportsUseID
        {
            get;
            private set;
        }

        /// <summary>
        /// Each physical HID device exposes a number of collections corresponding to logical devices.
        /// For a simple joystick, gamepad, etc. there will typically be one <see cref="DeviceItem"/>. Dual gamepad adapters will have two.
        /// Keyboards often have one for their keys and at least one for their volume and media controls.
        /// </summary>
        public IList<DeviceItem> DeviceItems
        {
            get;
            private set;
        }

        DescriptorCollectionItem RootItem
        {
            get { return State.RootItem; }
        }

        ReportDescriptorParseState State
        {
            get;
            set;
        }
    }
}
