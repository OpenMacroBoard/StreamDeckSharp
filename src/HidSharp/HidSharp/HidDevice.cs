#region License
/* Copyright 2010-2013, 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using HidSharp.Reports;

namespace HidSharp
{
    /// <summary>
    /// Represents a USB HID class device.
    /// </summary>
    [ComVisible(true), Guid("4D8A9A1A-D5CC-414e-8356-5A025EDA098D")]
    public abstract class HidDevice : Device
    {
        /// <inheritdoc/>
        public new HidStream Open()
        {
            return (HidStream)base.Open();
        }

        /// <inheritdoc/>
        public new HidStream Open(OpenConfiguration openConfig)
        {
            return (HidStream)base.Open(openConfig);
        }

        /// <inheritdoc/>
        public override string GetFriendlyName()
        {
            return GetProductName();
        }

        /// <summary>
        /// Returns the manufacturer name.
        /// </summary>
        public abstract string GetManufacturer();

        /// <summary>
        /// Returns the product name.
        /// </summary>
        public abstract string GetProductName();

        /// <summary>
        /// Returns the device serial number.
        /// </summary>
        public abstract string GetSerialNumber();

        /// <summary>
        /// Returns the maximum input report length, including the Report ID byte.
        /// If the device does not use Report IDs, the first byte will always be 0.
        /// </summary>
        public abstract int GetMaxInputReportLength();

        /// <summary>
        /// Returns the maximum output report length, including the Report ID byte.
        /// If the device does not use Report IDs, use 0 for the first byte.
        /// </summary>
        public abstract int GetMaxOutputReportLength();

        /// <summary>
        /// Returns the maximum feature report length, including the Report ID byte.
        /// If the device does not use Report IDs, use 0 for the first byte.
        /// </summary>
        public abstract int GetMaxFeatureReportLength();

        /// <summary>
        /// Retrieves and parses the report descriptor of the USB device.
        /// </summary>
        /// <returns>The parsed report descriptor.</returns>
        public ReportDescriptor GetReportDescriptor()
        {
            return new ReportDescriptor(GetRawReportDescriptor());
        }

        /// <summary>
        /// Returns the raw report descriptor of the USB device.
        /// </summary>
        /// <returns>The raw report descriptor.</returns>
        public virtual byte[] GetRawReportDescriptor()
        {
            throw new NotSupportedException(); // Windows reconstructs it. Linux can retrieve it. MacOS 10.8+ can retrieve it as well.
        }

        /*
        TODO
        public virtual string[] GetDevicePathHierarchy()
        {
            throw new NotSupportedException();
        }
        */

        /// <summary>
        /// Returns the serial ports of the composite USB device.
        /// Currently this is only supported on Windows.
        /// </summary>
        /// <returns>Serial ports of the USB device.</returns>
        public virtual string[] GetSerialPorts()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string manufacturer = "(unnamed manufacturer)";
            try { manufacturer = GetManufacturer(); } catch { }

            string productName = "(unnamed product)";
            try { productName = GetProductName(); } catch { }

            string serialNumber = "(no serial number)";
            try { serialNumber = GetSerialNumber(); } catch { }

            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} (VID {3}, PID {4}, version {5})",
                manufacturer, productName, serialNumber, VendorID, ProductID, ReleaseNumber);
        }

        /// <inheritdoc/>
        public bool TryOpen(out HidStream stream)
        {
            return TryOpen(null, out stream);
        }

        /// <inheritdoc/>
        public bool TryOpen(OpenConfiguration openConfig, out HidStream stream)
        {
            DeviceStream baseStream;
            bool result = base.TryOpen(openConfig, out baseStream);
            stream = (HidStream)baseStream; return result;
		}

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.HidDevice;
        }

        /// <summary>
        /// The USB product ID. These are listed at: http://usb-ids.gowdy.us
        /// </summary>
        public abstract int ProductID
        {
            get;
        }

        /// <summary>
        /// The device release number.
        /// </summary>
        public Version ReleaseNumber
        {
            get { return Utility.BcdHelper.ToVersion(ReleaseNumberBcd); }
        }

        /// <summary>
        /// The device release number, in binary-coded decimal.
        /// </summary>
        public abstract int ReleaseNumberBcd
        {
            get;
        }

        /// <exclude />
        [Obsolete("Use ReleaseNumberBcd instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual int ProductVersion
        {
            get { return ReleaseNumberBcd; }
        }

        /// <summary>
        /// The USB vendor ID. These are listed at: http://usb-ids.gowdy.us
        /// </summary>
        public abstract int VendorID
        {
            get;
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string Manufacturer
        {
            get
            {
                try
                {
                    return GetManufacturer() ?? "";
                }
                catch (IOException)
                {
                    return "";
                }
                catch (UnauthorizedAccessException)
                {
                    return "";
                }
            }
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string ProductName
        {
            get
            {
                try
                {
                    return GetProductName() ?? "";
                }
                catch (IOException)
                {
                    return "";
                }
                catch (UnauthorizedAccessException)
                {
                    return "";
                }
            }
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string SerialNumber
        {
            get
            {
                try
                {
                    return GetSerialNumber() ?? "";
                }
                catch (IOException)
                {
                    return "";
                }
                catch (UnauthorizedAccessException)
                {
                    return "";
                }
            }
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public virtual int MaxInputReportLength
        {
            get
            {
                try
                {
                    return GetMaxInputReportLength();
                }
                catch (IOException)
                {
                    return 0;
                }
                catch (UnauthorizedAccessException)
                {
                    return 0;
                }
            }
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public virtual int MaxOutputReportLength
        {
            get
            {
                try
                {
                    return GetMaxOutputReportLength();
                }
                catch (IOException)
                {
                    return 0;
                }
                catch (UnauthorizedAccessException)
                {
                    return 0;
                }
            }
        }

        /// <exclude />
        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public virtual int MaxFeatureReportLength
        {
            get
            {
                try
                {
                    return GetMaxFeatureReportLength();
                }
                catch (IOException)
                {
                    return 0;
                }
                catch (UnauthorizedAccessException)
                {
                    return 0;
                }
            }
        }
    }
}
