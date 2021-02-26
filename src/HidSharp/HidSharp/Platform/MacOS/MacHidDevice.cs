#region License
/* Copyright 2012-2015, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Platform.MacOS
{
    sealed class MacHidDevice : HidDevice
    {
        string _manufacturer;
        string _productName;
        string _serialNumber;
        int _vid, _pid, _version;
        int _maxInput, _maxOutput, _maxFeature;
        bool _reportsUseID;
        byte[] _reportDescriptor;
        NativeMethods.io_string_t _path;

        MacHidDevice()
        {

        }

        internal static MacHidDevice TryCreate(NativeMethods.io_string_t path)
        {
            var d = new MacHidDevice() { _path = path };

            var service = NativeMethods.IORegistryEntryFromPath(0, ref path).ToIOObject();
            if (!service.IsSet) { return null; }

            using (service)
            {
                int? vid = NativeMethods.IORegistryEntryGetCFProperty_Int(service, NativeMethods.kIOHIDVendorIDKey);
                int? pid = NativeMethods.IORegistryEntryGetCFProperty_Int(service, NativeMethods.kIOHIDProductIDKey);
                int? version = NativeMethods.IORegistryEntryGetCFProperty_Int(service, NativeMethods.kIOHIDVersionNumberKey);
                if (vid == null || pid == null || version == null) { return null; }

                // TODO: Craft the report descriptor from IOHIDElements so we can support it below OS X 10.8...
                //       Also, our report sizes aren't correct for the no-report-ID case without this...

                d._vid = (int)vid;
                d._pid = (int)pid;
                d._version = (int)version;
                d._maxInput = NativeMethods.IORegistryEntryGetCFProperty_Int(service, NativeMethods.kIOHIDMaxInputReportSizeKey) ?? 0;
                d._maxOutput = NativeMethods.IORegistryEntryGetCFProperty_Int(service, NativeMethods.kIOHIDMaxOutputReportSizeKey) ?? 0;
                d._maxFeature = NativeMethods.IORegistryEntryGetCFProperty_Int(service, NativeMethods.kIOHIDMaxFeatureReportSizeKey) ?? 0;
                d._manufacturer = NativeMethods.IORegistryEntryGetCFProperty_String(service, NativeMethods.kIOHIDManufacturerKey);
                d._productName = NativeMethods.IORegistryEntryGetCFProperty_String(service, NativeMethods.kIOHIDProductKey);
                d._serialNumber = NativeMethods.IORegistryEntryGetCFProperty_String(service, NativeMethods.kIOHIDSerialNumberKey);
                d._reportDescriptor = NativeMethods.IORegistryEntryGetCFProperty_Data(service, NativeMethods.kIOHIDReportDescriptorKey);
                if (d._maxInput == 0 && d._maxOutput == 0 && d._maxFeature == 0) { return null; }

                // Does this device use Report IDs? Let's find out.
                d._reportsUseID = false; bool hasInput = false, hasOutput = false, hasFeature = false;
                using (var device = NativeMethods.IOHIDDeviceCreate(IntPtr.Zero, service).ToCFType())
                {
                    if (!device.IsSet) { return null; }

                    using (var elementArray = NativeMethods.IOHIDDeviceCopyMatchingElements(device, IntPtr.Zero).ToCFType())
                    {
                        if (!elementArray.IsSet) { return null; }

                        int elementCount = checked((int)NativeMethods.CFArrayGetCount(elementArray));
                        for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
                        {
                            var element = NativeMethods.CFArrayGetValueAtIndex(elementArray, (IntPtr)elementIndex);
                            if (element == IntPtr.Zero) { continue; }

                            var elementType = NativeMethods.IOHIDElementGetType(element);
                            switch (elementType)
                            {
                                case NativeMethods.IOHIDElementType.InputMisc:
                                case NativeMethods.IOHIDElementType.InputButton:
                                case NativeMethods.IOHIDElementType.InputAxis:
                                case NativeMethods.IOHIDElementType.InputScanCodes:
                                    hasInput = true; break;

                                case NativeMethods.IOHIDElementType.Output:
                                    hasOutput = true; break;

                                case NativeMethods.IOHIDElementType.Feature:
                                    hasFeature = true; break;
                            }

                            if (NativeMethods.IOHIDElementGetReportID(element) != 0)
                            {
                                d._reportsUseID = true;
                            }
                        }
                    }
                }

                if (!d._reportsUseID)
                {
                    // It does not use Report IDs. MacOS's maximums do not include said Report ID.
                    if (d._maxInput != 0) { d._maxInput++; }
                    if (d._maxOutput != 0) { d._maxOutput++; }
                    if (d._maxFeature != 0) { d._maxFeature++; }
                }

                if (!hasInput) { d._maxInput = 0; }
                if (!hasOutput) { d._maxOutput = 0; }
                if (!hasFeature) { d._maxFeature = 0; }
            }

            return d;
        }

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            var stream = new MacHidStream(this);
            try { stream.Init(_path); return stream; }
            catch { stream.Close(); throw; }
        }

        public override int GetMaxInputReportLength()
        {
            return _maxInput;
        }

        public override int GetMaxOutputReportLength()
        {
            return _maxOutput;
        }

        public override int GetMaxFeatureReportLength()
        {
            return _maxFeature;
        }

        public override string GetManufacturer()
        {
            if (_manufacturer == null) { throw DeviceException.CreateIOException(this, "Unnamed manufacturer."); }
            return _manufacturer;
        }

        public override string GetProductName()
        {
            if (_productName == null) { throw DeviceException.CreateIOException(this, "Unnamed product."); }
            return _productName;
        }

        public override string GetSerialNumber()
        {
            if (_serialNumber == null) { throw DeviceException.CreateIOException(this, "No serial number."); }
            return _serialNumber;
        }

        public override string GetFileSystemName()
        {
            throw new NotSupportedException();
        }

        public override byte[] GetRawReportDescriptor()
        {
            var descriptor = _reportDescriptor;
            if (descriptor == null) { throw new NotSupportedException("Report descriptors are only available on OS X 10.8+."); } // FIXME: Is this true? Found the minimum version via Google: https://codereview.chromium.org/1373923003
            return (byte[])descriptor.Clone();
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.MacOS;
        }

        public override string DevicePath
        {
            get { return _path.ToString(); }
        }

        public override int VendorID
        {
            get { return _vid; }
        }

        public override int ProductID
        {
            get { return _pid; }
        }

        public override int ReleaseNumberBcd
        {
            get { return _version; }
        }

        internal bool ReportsUseID
        {
            get { return _reportsUseID; }
        }
    }
}
