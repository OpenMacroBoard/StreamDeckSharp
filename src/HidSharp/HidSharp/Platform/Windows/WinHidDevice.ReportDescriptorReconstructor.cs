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
using System.IO;
using System.Linq;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;

namespace HidSharp.Platform.Windows
{
    partial class WinHidDevice
    {
        // TODO: This likely will only work for very simple descriptors. Extend its functionality over time as needed...
        struct ReportDescriptorReconstructor
        {
            ReportDescriptorBuilder _builder;
            IntPtr _preparsed;
            List<ushort> _currentNodes;
            NativeMethods.HIDP_LINK_COLLECTION_NODE[] _nodes;
            sealed class ItemCaps { public bool Button; public int BitOffset, ReportCount, ReportSize; public NativeMethods.HIDP_DATA_CAPS Item; }
            sealed class ReportCaps { public ItemCaps[] Items; public int ReportLength; }
            ReportCaps[] _types;

            static void InitData(byte[] report, byte reportID)
            {
                Array.Clear(report, 0, report.Length);
                report[0] = (byte)reportID;
            }

            static bool GetDataBitValue(byte[] report, int bit)
            {
                return (report[bit >> 3] & (1 << (bit & 7))) != 0;
            }

            void GetDataStartBit(byte[] report, ItemCaps item, int maxBit)
            {
                int startBit; // TODO: Make this much more efficient.
                for (startBit = 0; startBit < maxBit && !GetDataBitValue(report, startBit + 8); startBit++) ;
                item.BitOffset = startBit;
            }

            void AddButtonGlobalItems(int bitCount)
            {
                _builder.AddGlobalItemSigned(GlobalItemTag.LogicalMinimum, 0);
                _builder.AddGlobalItemSigned(GlobalItemTag.LogicalMaximum, 1);
                _builder.AddGlobalItemSigned(GlobalItemTag.PhysicalMinimum, 0);
                _builder.AddGlobalItemSigned(GlobalItemTag.PhysicalMaximum, 1);
                _builder.AddGlobalItem(GlobalItemTag.Unit, 0);
                _builder.AddGlobalItem(GlobalItemTag.UnitExponent, 0);
                _builder.AddGlobalItem(GlobalItemTag.ReportSize, 1);
                _builder.AddGlobalItem(GlobalItemTag.ReportCount, (uint)bitCount);
            }

            void PadReport(MainItemTag mainItemTag, int padToBit, ref int currentBit)
            {
                if (currentBit > padToBit) { throw new NotImplementedException(); } // Overlapping...

                // Padding...
                int paddingBitCount = padToBit - currentBit;
                if (paddingBitCount > 0)
                {
                    AddButtonGlobalItems(paddingBitCount);
                    _builder.AddMainItem(mainItemTag, (uint)(DataItemFlags.Constant | DataItemFlags.Variable));
                    currentBit += paddingBitCount;
                }
            }

            void EncodeReports(NativeMethods.HIDP_REPORT_TYPE reportType, MainItemTag mainItemTag)
            {
                var types = _types[(int)reportType];
                var reportBytes = new byte[types.ReportLength];

                var reports = types.Items.GroupBy(y => y.Item.ReportID);
                foreach (var report in reports)
                {
                    var reportID = report.Key;
                    var reportItemList = report.ToArray();

                    if (reportID != 0)
                    {
                        _builder.AddGlobalItem(GlobalItemTag.ReportID, reportID);
                    }

                    int maxBit = (reportBytes.Length - 1) * 8;

                    // Determine the location of all report items.
                    for (int reportItemIndex = 0; reportItemIndex < reportItemList.Length; reportItemIndex ++)
                    {
                        var reportItem = reportItemList[reportItemIndex];

                        var button = reportItem.Button;
                        var item = reportItem.Item;
                        if (item.IsAlias != 0) { throw new NotImplementedException(); }
                        var flags = (DataItemFlags)item.BitField;

                        int dataIndexCount = (item.IsRange != 0) ? item.DataIndexMax - item.DataIndex + 1 : 1;
                        if (button)
                        {
                            reportItem.ReportCount = dataIndexCount;
                            reportItem.ReportSize = 1;
                        }
                        else
                        {
                            reportItem.ReportCount = item.VALUE_ReportCount;
                            reportItem.ReportSize = item.VALUE_ReportSize;
                        }

                        InitData(reportBytes, reportID);
                        if (dataIndexCount == reportItem.ReportCount)
                        {
                            // Individual fields...
                            var dataList = new NativeMethods.HIDP_DATA() { DataIndex = item.DataIndex, RawValue = 0xffffffffu }; int dataCount = 1;
                            int hr = NativeMethods.HidP_SetData(reportType, ref dataList, ref dataCount, _preparsed, reportBytes, reportBytes.Length);
                            if (hr == NativeMethods.HIDP_STATUS_SUCCESS)
                            {
                                GetDataStartBit(reportBytes, reportItem, maxBit);
                            }
                            else if (hr == NativeMethods.HIDP_STATUS_IS_VALUE_ARRAY)
                            {
                                // TODO
                                reportItem.BitOffset = maxBit;

                                // According to https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/button-capability-arrays#button-usages-in-a-variable-main-item:
                                /*
                                    If the number of usages specified for a variable item is less than the number of buttons in the item,
                                    the capability array contains only one capability structure that describes one button usage (the last
                                    usage specified in the report descriptor for the variable main item). However, see Usage Value Array
                                    for information about usage values that have a report count greater than one.
                                */
                                // How to reconstruct it, then? The following does not work:
                                /*
                                var usage = (ushort)item.UsageIndex; int usageCount = 1;
                                hr = NativeMethods.HidP_SetUsages(reportType, item.UsagePage, 0, ref usage, ref usageCount, _preparsed, reportBytes, reportBytes.Length);
                                if (hr != NativeMethods.HIDP_STATUS_SUCCESS) { throw new NotImplementedException(); }
                                */
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }                            
                        }
                        else if (dataIndexCount == 1)
                        {
                            // Array...
                            int bitCount = reportItem.ReportCount * reportItem.ReportSize;
                            var usageValue = new byte[(bitCount + 7) / 8];
                            for (int i = 0; i < usageValue.Length; i++) { usageValue[i] = 0xff; }
                            int hr = NativeMethods.HidP_SetUsageValueArray(reportType, item.UsagePage, item.LinkCollection, item.UsageIndex, usageValue, (ushort)usageValue.Length, _preparsed, reportBytes, reportBytes.Length);
                            if (hr == NativeMethods.HIDP_STATUS_SUCCESS)
                            {
                                GetDataStartBit(reportBytes, reportItem, maxBit);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            // Not sure...
                            reportItem.BitOffset = maxBit;
                        }
                    }

                    // Write the report descriptors.
                    int currentBit = 0;

                    var reportItems = report.Where(x => x.BitOffset != maxBit).OrderBy(x => x.BitOffset).ToArray();
                    foreach (var reportItem in reportItems)
                    {
                        var button = reportItem.Button;
                        var item = reportItem.Item;
                        int startBit = reportItem.BitOffset;
                        int bitCount = reportItem.ReportCount * reportItem.ReportSize;
                        if (currentBit > startBit) { throw new NotImplementedException(); } // Overlapping...

                        SetCollection(item.LinkCollection);
                        PadReport(mainItemTag, startBit, ref currentBit);

                        // The entry...
                        _builder.AddGlobalItem(GlobalItemTag.UsagePage, item.UsagePage);

                        uint usageMin = item.UsageIndex, usageMax = (item.IsRange != 0) ? item.UsageMax : usageMin;
                        if (item.IsRange != 0)
                        {
                            _builder.AddLocalItem(LocalItemTag.UsageMinimum, usageMin);
                            _builder.AddLocalItem(LocalItemTag.UsageMaximum, usageMax);
                        }
                        else
                        {
                            _builder.AddLocalItem(LocalItemTag.Usage, usageMin);
                        }

                        if (button)
                        {
                            AddButtonGlobalItems(reportItem.ReportCount);
                        }
                        else
                        {
                            _builder.AddGlobalItemSigned(GlobalItemTag.LogicalMinimum, item.VALUE_LogicalMin);
                            _builder.AddGlobalItemSigned(GlobalItemTag.LogicalMaximum, item.VALUE_LogicalMax);
                            _builder.AddGlobalItemSigned(GlobalItemTag.PhysicalMinimum, item.VALUE_PhysicalMin);
                            _builder.AddGlobalItemSigned(GlobalItemTag.PhysicalMaximum, item.VALUE_PhysicalMax);
                            _builder.AddGlobalItem(GlobalItemTag.Unit, item.VALUE_Units);
                            _builder.AddGlobalItem(GlobalItemTag.UnitExponent, item.VALUE_UnitsExp);
                            _builder.AddGlobalItem(GlobalItemTag.ReportSize, (uint)reportItem.ReportSize);
                            _builder.AddGlobalItem(GlobalItemTag.ReportCount, (uint)reportItem.ReportCount);
                        }
                        _builder.AddMainItem(mainItemTag, item.BitField);

                        currentBit += bitCount;
                    }

                    PadReport(mainItemTag, maxBit, ref currentBit);
                }
            }

            void BeginCollection(ushort nodeIndex)
            {
                var node = _nodes[nodeIndex];
                if (node.IsAlias != 0) { throw new NotImplementedException(); }

                _builder.AddGlobalItem(GlobalItemTag.UsagePage, node.LinkUsagePage);
                _builder.AddLocalItem(LocalItemTag.Usage, node.LinkUsage);
                _builder.AddMainItem(MainItemTag.Collection, node.CollectionType);
            }

            void EndCollection()
            {
                _builder.AddMainItem(MainItemTag.EndCollection, 0);
            }


            void SetCollection(List<ushort> newNodes)
            {
                int countToCheck = Math.Min(_currentNodes.Count, newNodes.Count);
                int sharedCount;
                for (sharedCount = 0; sharedCount < countToCheck;  sharedCount++)
                {
                    if (_currentNodes[sharedCount] != newNodes[sharedCount]) { break; }
                }

                while (_currentNodes.Count > sharedCount)
                {
                    EndCollection(); _currentNodes.RemoveAt(_currentNodes.Count - 1);
                }

                for (int i = sharedCount; i < newNodes.Count; i++)
                {
                    ushort nodeIndex = newNodes[i]; _currentNodes.Add(nodeIndex); BeginCollection(nodeIndex);
                }
            }

            void SetCollection(ushort nodeIndex)
            {
                var nodes = _currentNodes;
                if (nodes.Count >= 1 && nodes[nodes.Count - 1] == nodeIndex) { return; }

                var newNodes = new List<ushort>();
                while (true)
                {
                    newNodes.Add(nodeIndex);
                    if (nodeIndex == 0) { break; }
                    nodeIndex = _nodes[nodeIndex].Parent;
                }
                newNodes.Reverse();

                SetCollection(newNodes);
            }

            void GetReportCaps(NativeMethods.HIDP_REPORT_TYPE reportType, ushort buttonCount, ushort valueCount, ushort reportLength)
            {
                var caps = new ReportCaps(); ushort count;
                var buttons = new NativeMethods.HIDP_DATA_CAPS[buttonCount];
                var values = new NativeMethods.HIDP_DATA_CAPS[valueCount];

                count = buttonCount;
                if (count > 0)
                {
                    if (NativeMethods.HidP_GetButtonCaps(reportType, buttons, ref count, _preparsed) != NativeMethods.HIDP_STATUS_SUCCESS || count != buttonCount) { throw new NotImplementedException(); }
                }

                count = valueCount;
                if (count > 0)
                {
                    if (NativeMethods.HidP_GetValueCaps(reportType, values, ref count, _preparsed) != NativeMethods.HIDP_STATUS_SUCCESS || count != valueCount) { throw new NotImplementedException(); }
                }
                
                caps.Items = buttons.Select(b => new ItemCaps() { Button = true, Item = b })
                    .Concat(values.Select(v => new ItemCaps() { Button = false, Item = v }))
                    .ToArray();
                caps.ReportLength = reportLength;
                _types[(int)reportType] = caps;
            }

            public byte[] Run(IntPtr preparsed, NativeMethods.HIDP_CAPS caps)
            {
                _builder = new ReportDescriptorBuilder();
                _preparsed = preparsed;

                _nodes = new NativeMethods.HIDP_LINK_COLLECTION_NODE[caps.NumberLinkCollectionNodes]; int nodeCount = _nodes.Length;
                if (NativeMethods.HidP_GetLinkCollectionNodes(_nodes, ref nodeCount, preparsed) != NativeMethods.HIDP_STATUS_SUCCESS || nodeCount != _nodes.Length) { throw new NotImplementedException(); }

                _types = new ReportCaps[(int)NativeMethods.HIDP_REPORT_TYPE.Count];
                GetReportCaps(NativeMethods.HIDP_REPORT_TYPE.Input, caps.NumberInputButtonCaps, caps.NumberInputValueCaps, caps.InputReportByteLength);
                GetReportCaps(NativeMethods.HIDP_REPORT_TYPE.Output, caps.NumberOutputButtonCaps, caps.NumberOutputValueCaps, caps.OutputReportByteLength);
                GetReportCaps(NativeMethods.HIDP_REPORT_TYPE.Feature, caps.NumberFeatureButtonCaps, caps.NumberFeatureValueCaps, caps.FeatureReportByteLength);

                _currentNodes = new List<ushort>();
                SetCollection(new List<ushort>() { 0 });
                EncodeReports(NativeMethods.HIDP_REPORT_TYPE.Input, MainItemTag.Input);
                EncodeReports(NativeMethods.HIDP_REPORT_TYPE.Output, MainItemTag.Output);
                EncodeReports(NativeMethods.HIDP_REPORT_TYPE.Feature, MainItemTag.Feature);
                SetCollection(new List<ushort>());

                return _builder.GetReportDescriptor();
            }
        }
    }
}
