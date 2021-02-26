#region License
/* Copyright 2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Runtime.InteropServices;
using HidSharp.Experimental;

namespace HidSharp.Platform.Windows
{
    sealed class WinBleDevice : BleDevice
    {
        string _path, _id;
        string _friendlyName;
        WinBleService[] _services;
        object _syncObject;

        internal static WinBleDevice TryCreate(string path, string id, string friendlyName)
        {
            var d = new WinBleDevice() { _path = path, _id = id, _friendlyName = friendlyName, _syncObject = new object() };
            return d;
        }

        internal bool TryOpenToGetInfo(Func<IntPtr, bool> action)
        {
            return NativeMethods.TryOpenToGetInfo(_path, action);
        }

        WinBleService GetService(OpenConfiguration openConfig)
        {
            return (WinBleService)openConfig.GetOption(OpenOption.BleService);
        }

        protected override string GetStreamPath(OpenConfiguration openConfig)
        {
            var service = GetService(openConfig);
            if (service == null) { throw DeviceException.CreateIOException(this, "BLE service not specified."); }
            if (service.Device != this) { throw DeviceException.CreateIOException(this, "BLE service is on a different device."); }

            uint devInst;
            if (0 == NativeMethods.CM_Locate_DevNode(out devInst, _id))
            {
                if (0 == NativeMethods.CM_Get_Child(out devInst, devInst))
                {
                    do
                    {
                        string serviceDeviceID;
                        if (0 == NativeMethods.CM_Get_Device_ID(devInst, out serviceDeviceID))
                        {
                            NativeMethods.HDEVINFO devInfo = NativeMethods.SetupDiGetClassDevs(
                                service.Uuid, serviceDeviceID, IntPtr.Zero,
                                NativeMethods.DIGCF.DeviceInterface | NativeMethods.DIGCF.Present
                                );

                            if (devInfo.IsValid)
                            {
                                try
                                {
                                    NativeMethods.SP_DEVINFO_DATA dvi = new NativeMethods.SP_DEVINFO_DATA();
                                    dvi.Size = Marshal.SizeOf(dvi);

                                    for (int j = 0; NativeMethods.SetupDiEnumDeviceInfo(devInfo, j, ref dvi); j++)
                                    {
                                        NativeMethods.SP_DEVICE_INTERFACE_DATA did = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                                        did.Size = Marshal.SizeOf(did);

                                        for (int k = 0; NativeMethods.SetupDiEnumDeviceInterfaces(devInfo, ref dvi, service.Uuid, k, ref did); k++)
                                        {
                                            string devicePath;
                                            if (NativeMethods.SetupDiGetDeviceInterfaceDevicePath(devInfo, ref did, out devicePath))
                                            {
                                                // FIXME: Take the attribute handle into account as well.
                                                //        Right now, if there are multiple services with the same GUID, we do not distinguish between them.
                                                return devicePath;
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    NativeMethods.SetupDiDestroyDeviceInfoList(devInfo);
                                }
                            }
                        }
                    }
                    while (0 == NativeMethods.CM_Get_Sibling(out devInst, devInst));
                }
            }

            throw DeviceException.CreateIOException(this, string.Format("BLE service {0} not found.", service.Uuid));
        }

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            var streamPath = GetStreamPath(openConfig);

            var stream = new WinBleStream(this, GetService(openConfig));
            try { stream.Init(streamPath); return stream; }
            catch { stream.Close(); throw; }
        }

        /*
        public override bool GetConnectionState()
        {
            uint devInst;
            if (0 == NativeMethods.CM_Locate_DevNode(out devInst, _id))
            {
                uint dnStatus, dnProblemNumber;
                if (0 == NativeMethods.CM_Get_DevNode_Status(out dnStatus, out dnProblemNumber, devInst))
                {
                    if (0 == (dnStatus & NativeMethods.DN_DEVICE_DISCONNECTED)) { return true; }
                }
            }

            return false;
        }
        */

        void RequiresServices()
        {
            lock (_syncObject)
            {
                if (!TryOpenToGetInfo(handle =>
                {
                    var nativeServices = NativeMethods.BluetoothGATTGetServices(handle);
                    if (nativeServices == null) { return false; }

                    var services = new List<WinBleService>();
                    foreach (var nativeService in nativeServices)
                    {
                        var service = new WinBleService(this, nativeService);
                        services.Add(service);
                    }

                    _services = services.ToArray();
                    return true;
                }))
                {
                    throw DeviceException.CreateIOException(this, "BLE service list could not be retrieved.");
                }
            }
        }

        public override BleService[] GetServices()
        {
            RequiresServices();
            return (BleService[])_services.Clone();
        }

        public override bool HasService(BleUuid service)
        {
            RequiresServices();
            var services = _services;

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i].Uuid == service) { return true; }
            }

            return false;
        }

        public override bool TryGetService(BleUuid guid, out BleService service)
        {
            RequiresServices();
            var services = _services;

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i].Uuid == guid) { service = services[i]; return true; }
            }

            service = null; return false;
        }

        public override string GetFriendlyName()
        {
            return _friendlyName;
        }

        public override string GetFileSystemName()
        {
            return DevicePath;
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.Windows;
        }

        public override string DevicePath
        {
            get { return _path; }
        }
    }
}
