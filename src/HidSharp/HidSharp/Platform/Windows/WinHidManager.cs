#region License
/* Copyright 2012-2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

// TODO: The individual radio notifications only seem to happen after we've already connected.
//       If we can find a way to force discovery mode for BLE devices, by all means enable this.
//#define BLUETOOTH_NOTIFY

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using HidSharp.Experimental;
using HidSharp.Utility;
using Microsoft.Win32;

namespace HidSharp.Platform.Windows
{
    sealed class WinHidManager : HidManager
    {
        #region Device Paths
        class DevicePathBase
        {
            public override bool Equals(object obj)
            {
                var path = obj as DevicePathBase;
                return path != null && DevicePath == path.DevicePath && DeviceID == path.DeviceID;
            }

            public override int GetHashCode()
            {
                return DevicePath.GetHashCode();
            }

            public override string ToString()
            {
                return DevicePath;
            }

            public string DevicePath;
            public string DeviceID;
        }

        sealed class BleDevicePath : DevicePathBase
        {
            public override bool Equals(object obj)
            {
                var path = obj as BleDevicePath;
                return path != null && FriendlyName == path.FriendlyName;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public string FriendlyName;
        }

        sealed class HidDevicePath : DevicePathBase
        {

        }

        sealed class SerialDevicePath : DevicePathBase
        {
            public override bool Equals(object obj)
            {
                var path = obj as SerialDevicePath;
                return path != null && FileSystemName == path.FileSystemName && FriendlyName == path.FriendlyName;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public string FileSystemName;
            public string FriendlyName;
        }
        #endregion

        bool _isSupported;
        bool _bleIsSupported;

        //static Thread _bleDiscoveryThread;
        //static int _bleDiscoveryRefCount;
        //static volatile bool _bleDiscoveryShuttingDown;

        static Thread _serialWatcherThread;
        static IntPtr _serialWatcherShutdownEvent;

        static Thread _notifyThread;
        static volatile bool _notifyThreadShouldNotify;
        static volatile bool _notifyThreadShuttingDown;

        static object _hidNotifyObject;
        static object _serNotifyObject;
        static object _bleNotifyObject;

        static object[] _hidDeviceKeysCache;
        static object[] _serDeviceKeysCache;
        static object[] _bleDeviceKeysCache;
        static object _hidDeviceKeysCacheNotifyObject;
        static object _serDeviceKeysCacheNotifyObject;
        static object _bleDeviceKeysCacheNotifyObject;

        public WinHidManager()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var version = new NativeMethods.OSVERSIONINFO();
                version.OSVersionInfoSize = Marshal.SizeOf(typeof(NativeMethods.OSVERSIONINFO));

                try
                {
                    if (NativeMethods.GetVersionEx(ref version) && version.PlatformID == 2)
                    {
                        _isSupported = true;

                        if (Environment.OSVersion.Version >= new Version(6, 2))
                        {
                            _bleIsSupported = true;
                        }
                    }
                }
                catch
                {
                    // Apparently we have no P/Invoke access.
                }
            }
        }

#if BLUETOOTH_NOTIFY
        struct BleRadio { public IntPtr NotifyHandle, RadioHandle; }
#endif
        protected override void Run(Action readyCallback)
        {
            const string className = "HidSharpDeviceMonitor";

            NativeMethods.WindowProc windowProc = DeviceMonitorWindowProc;
            var wc = new NativeMethods.WNDCLASS() { ClassName = className, WindowProc = windowProc };
            RunAssert(0 != NativeMethods.RegisterClass(ref wc), "HidSharp RegisterClass failed.");

            var hwnd = NativeMethods.CreateWindowEx(0, className, className, 0,
                                                    NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT,
                                                    NativeMethods.HWND_MESSAGE,
                                                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            RunAssert(hwnd != IntPtr.Zero, "HidSharp CreateWindow failed.");

            var hidNotifyHandle = RegisterDeviceNotification(hwnd, NativeMethods.HidD_GetHidGuid());
            var bleNotifyHandle = RegisterDeviceNotification(hwnd, NativeMethods.GuidForBluetoothLEDevice);

#if BLUETOOTH_NOTIFY
            var bleHandles = new List<BleRadio>(); // FIXME: We don't handle the removal of USB Bluetooth dongles here, as far as notifications go.

            IntPtr searchHandle, radioHandle;
            var searchParams = new NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS() { Size = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS)) };

            searchHandle = NativeMethods.BluetoothFindFirstRadio(ref searchParams, out radioHandle);
            if (searchHandle != IntPtr.Zero)
            {
                do
                {
                    var radio = new BleRadio();
                    radio.RadioHandle = radioHandle;
                    radio.NotifyHandle = RegisterDeviceNotification(hwnd, radioHandle);
                    bleHandles.Add(radio);
                }
                while (NativeMethods.BluetoothFindNextRadio(searchHandle, out radioHandle));

                NativeMethods.BluetoothFindRadioClose(searchHandle);
            }

            if (bleHandles.Count > 0)
            {
                HidSharpDiagnostics.Trace("Found {0} Bluetooth radio(s).", bleHandles.Count);
            }
#endif

            //_bleDiscoveryThread = new Thread(BleDiscoveryThread) { IsBackground = true, Name = "HidSharp BLE Discovery" };
            //_bleDiscoveryThread.Start();

            _serialWatcherShutdownEvent = NativeMethods.CreateManualResetEventOrThrow();
            _serialWatcherThread = new Thread(SerialWatcherThread) { IsBackground = true, Name = "HidSharp Serial Watcher" };
            _serialWatcherThread.Start();

            _hidNotifyObject = new object();
            _serNotifyObject = new object();
            _bleNotifyObject = new object();
            _notifyThread = new Thread(DeviceMonitorEventThread) { IsBackground = true, Name = "HidSharp RaiseChanged" };
            _notifyThread.Start();

            readyCallback();

            NativeMethods.MSG msg;
            while (true)
            {
                int result = NativeMethods.GetMessage(out msg, hwnd, 0, 0);
                if (result == 0 || result == -1) { break; }

                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }

            //lock (_bleDiscoveryThread) { _bleDiscoveryShuttingDown = true; Monitor.Pulse(_bleDiscoveryThread); }
            lock (_notifyThread) { _notifyThreadShuttingDown = true; Monitor.Pulse(_notifyThread); }
            NativeMethods.SetEvent(_serialWatcherShutdownEvent);
            //_bleDiscoveryThread.Join();
            _notifyThread.Join();
            _serialWatcherThread.Join();

            UnregisterDeviceNotification(hidNotifyHandle);
            UnregisterDeviceNotification(bleNotifyHandle);
#if BLUETOOTH_NOTIFY
            foreach (var bleHandle in bleHandles) { UnregisterDeviceNotification(bleHandle.NotifyHandle); NativeMethods.CloseHandle(bleHandle.RadioHandle); }
#endif

            RunAssert(NativeMethods.DestroyWindow(hwnd), "HidSharp DestroyWindow failed.");
            RunAssert(NativeMethods.UnregisterClass(className, IntPtr.Zero), "HidSharp UnregisterClass failed.");
            GC.KeepAlive(windowProc);
        }

        static IntPtr  RegisterDeviceNotification(IntPtr hwnd, Guid guid)
        {
            var notifyFilter = new NativeMethods.DEV_BROADCAST_DEVICEINTERFACE()
            {
                Size = Marshal.SizeOf(typeof(NativeMethods.DEV_BROADCAST_DEVICEINTERFACE)),
                ClassGuid = guid,
                DeviceType = NativeMethods.DBT_DEVTYP_DEVICEINTERFACE
            };
            var notifyHandle = NativeMethods.RegisterDeviceNotification(hwnd, ref notifyFilter, 0);
            RunAssert(notifyHandle != IntPtr.Zero, "HidSharp RegisterDeviceNotification failed.");
            return notifyHandle;
        }

        static IntPtr RegisterDeviceNotification(IntPtr hwnd, IntPtr handle)
        {
            var notifyFilter = new NativeMethods.DEV_BROADCAST_HANDLE()
            {
                Size = Marshal.SizeOf(typeof(NativeMethods.DEV_BROADCAST_HANDLE)),
                DeviceHandle = handle,
                DeviceType = NativeMethods.DBT_DEVTYP_HANDLE
            };
            var notifyHandle = NativeMethods.RegisterDeviceNotification(hwnd, ref notifyFilter, 0);
            RunAssert(notifyHandle != IntPtr.Zero, "HidSharp RegisterDeviceNotification failed.");
            return notifyHandle;
        }

        static void UnregisterDeviceNotification(IntPtr handle)
        {
            RunAssert(NativeMethods.UnregisterDeviceNotification(handle), "HidSharp UnregisterDeviceNotification failed.");
        }

        unsafe static IntPtr DeviceMonitorWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (message == NativeMethods.WM_DEVICECHANGE)
            {
                var ev = (NativeMethods.WM_DEVICECHANGE_wParam)(int)(long)wParam;
                HidSharpDiagnostics.Trace("Received a device change event, {0}.", ev);

                var eventArgs = (NativeMethods.DEV_BROADCAST_HDR*)(void*)lParam;

                if (ev == NativeMethods.WM_DEVICECHANGE_wParam.DBT_DEVICEARRIVAL || ev == NativeMethods.WM_DEVICECHANGE_wParam.DBT_DEVICEREMOVECOMPLETE)
                {
                    if (eventArgs->DeviceType == NativeMethods.DBT_DEVTYP_DEVICEINTERFACE)
                    {
                        var diEventArgs = (NativeMethods.DEV_BROADCAST_DEVICEINTERFACE*)eventArgs;

                        if (diEventArgs->ClassGuid == NativeMethods.HidD_GetHidGuid())
                        {
                            DeviceListDidChange(ref _hidNotifyObject);
                        }
                        else if (diEventArgs->ClassGuid == NativeMethods.GuidForBluetoothLEDevice)
                        {
                            DeviceListDidChange(ref _bleNotifyObject);
                        }
                    }
                }
                else if (ev == NativeMethods.WM_DEVICECHANGE_wParam.DBT_CUSTOMEVENT)
                {
                    if (eventArgs->DeviceType == NativeMethods.DBT_DEVTYP_HANDLE)
                    {
                        var handleEventArgs = (NativeMethods.DEV_BROADCAST_HANDLE*)eventArgs;

                        if (handleEventArgs->EventGuid == NativeMethods.GuidForBluetoothHciEvent)
                        {
                            var hciEvent = (NativeMethods.BTH_HCI_EVENT_INFO*)&handleEventArgs->Data[0];
                            HidSharpDiagnostics.Trace("Bluetooth HCI event: address {0:X}, type {1}, connected {2}",
                                                      hciEvent->bthAddress, hciEvent->connectionType, hciEvent->connected);
                        }
                        else if (handleEventArgs->EventGuid == NativeMethods.GuidForBluetoothRadioInRange)
                        {
                            var radioInRange = (NativeMethods.BTH_RADIO_IN_RANGE*)&handleEventArgs->Data[0];
                            HidSharpDiagnostics.Trace("Radio in range event: address {0:X}, flags {1}, class {2}, name '{3}{4}'",
                                radioInRange->deviceInfo.address,
                                radioInRange->deviceInfo.flags,
                                radioInRange->deviceInfo.classOfDevice,
                                (char)radioInRange->deviceInfo.name[0],
                                (char)radioInRange->deviceInfo.name[1]);
                        }
                        else if (handleEventArgs->EventGuid == NativeMethods.GuidForBluetoothRadioOutOfRange)
                        {
                            var address = (NativeMethods.BLUETOOTH_ADDRESS*)&handleEventArgs->Data[0];
                            HidSharpDiagnostics.Trace("Radio out of range event: address {0:X}",
                                address->Addr);
                        }
                        else
                        {
                            HidSharpDiagnostics.Trace("Custom event: GUID {0}", handleEventArgs->EventGuid);
                        }

                        // For now, this doesn't raise the DeviceList Changed event. It only seems to occur at connection.
                    }
                }
                
                return (IntPtr)1;
            }

            return NativeMethods.DefWindowProc(window, message, wParam, lParam);
        }

        static void DeviceListDidChange(ref object notifyObject)
        {
            lock (_notifyThread)
            {
                notifyObject = new object();
                _notifyThreadShouldNotify = true;
                Monitor.Pulse(_notifyThread);
            }
        }

        // usbser.sys does not register an interface, at least on Windows 7 and earlier. (Other serial drivers don't suffer from this flaw.)
        // In any case, to detect connections and disconnections, let's watch the SERIALCOMM registry key.
        static unsafe void SerialWatcherThread()
        {
            var notifyEvent = NativeMethods.CreateAutoResetEventOrThrow();

            try
            {
                IntPtr handle;
                if (0 == NativeMethods.RegOpenKeyEx(new IntPtr(unchecked((int)NativeMethods.HKEY_LOCAL_MACHINE)), @"HARDWARE\DEVICEMAP\SERIALCOMM", 0, NativeMethods.KEY_NOTIFY, out handle))
                {
                    try
                    {
                        var handles = stackalloc IntPtr[2];
                        handles[0] = _serialWatcherShutdownEvent;
                        handles[1] = notifyEvent;

                        while (true)
                        {
                            if (0 != NativeMethods.RegNotifyChangeKeyValue(handle, false, NativeMethods.REG_NOTIFY_CHANGE_LAST_SET, notifyEvent, true)) { break; }

                            switch (NativeMethods.WaitForMultipleObjects(2, handles, false, uint.MaxValue))
                            {
                                case NativeMethods.WAIT_OBJECT_0: default:
                                    return;

                                case NativeMethods.WAIT_OBJECT_1:
                                    HidSharpDiagnostics.Trace("Received a serial change event.");
                                    DeviceListDidChange(ref _serNotifyObject); break;
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.RegCloseKey(handle);
                    }
                }
            }
            finally
            {
                NativeMethods.CloseHandle(notifyEvent);
            }
        }

        /*
        static void BleDiscoveryThread()
        {
            try
            {
                lock (_bleDiscoveryThread)
                {
                    while (true)
                    {
                        if (_bleDiscoveryShuttingDown)
                        {
                            break;
                        }

                        if (_bleDiscoveryRefCount != 0)
                        {
                            Monitor.Exit(_bleDiscoveryThread);

                            try
                            {
                                HidSharpDiagnostics.Trace("Performing Bluetooth discovery...");

                                var @params = new NativeMethods.BLUETOOTH_DEVICE_SEARCH_PARAMS();
                                @params.dwSize = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_DEVICE_SEARCH_PARAMS));
                                @params.fReturnAuthenticated = 1;
                                @params.fReturnConnected = 1;
                                @params.fReturnRemembered = 1;
                                @params.fReturnUnknown = 1;
                                @params.fIssueInquiry = 1;
                                @params.cTimeoutMultiplier = 4; // 5.12 seconds

                                var info = new NativeMethods.BLUETOOTH_DEVICE_INFO();
                                info.dwSize = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_DEVICE_INFO));

                                var search = NativeMethods.BluetoothFindFirstDevice(ref @params, ref info);
                                if (search != IntPtr.Zero)
                                {
                                    HidSharpDiagnostics.Trace("Bluetooth devices enumerated.");
                                    NativeMethods.BluetoothFindDeviceClose(search);
                                    continue;
                                }
                                else
                                {
                                    int error = Marshal.GetLastWin32Error();
                                    if (error == NativeMethods.ERROR_NO_MORE_ITEMS)
                                    {
                                        HidSharpDiagnostics.Trace("No Bluetooth devices enumerable. (That's okay. We are just trying to activate discovery.)");
                                        continue;
                                    }
                                    else
                                    {
                                        HidSharpDiagnostics.Trace("Win32 error {0} while enumerating Bluetooth devices.", error);
                                    }
                                }
                            }
                            finally
                            {
                                Monitor.Enter(_bleDiscoveryThread);
                            }
                        }

                        Monitor.Wait(_bleDiscoveryThread);
                    }
                }
            }
            catch (Exception e)
            {
                HidSharpDiagnostics.Trace("Bluetooth discovery thread failed with exception: {0}", e);
            }
        }
        */

        static void DeviceMonitorEventThread()
        {
            lock (_notifyThread)
            {
                while (true)
                {
                    if (_notifyThreadShuttingDown)
                    {
                        break;
                    }
                    else if (_notifyThreadShouldNotify)
                    {
                        _notifyThreadShouldNotify = false;

                        Monitor.Exit(_notifyThread);
                        try
                        {
                            DeviceList.Local.RaiseChanged();
                        }
                        finally
                        {
                            Monitor.Enter(_notifyThread);
                        }
                    }
                    else
                    {
                        Monitor.Wait(_notifyThread);
                    }
                }
            }
        }

        /*
        sealed class WinBleDiscovery : BleDiscovery
        {
            int _stopped;

            ~WinBleDiscovery()
            {
                StopDiscovery();
            }

            public override void StopDiscovery()
            {
                if (Interlocked.Exchange(ref _stopped, 1) == 0)
                {
                    lock (_bleDiscoveryThread)
                    {
                        Debug.Assert(_bleDiscoveryRefCount > 0);

                        _bleDiscoveryRefCount--;
                        Monitor.Pulse(_bleDiscoveryThread);
                    }
                }
            }
        }

        public override BleDiscovery BeginBleDiscovery()
        {
            lock (_bleDiscoveryThread)
            {
                checked { _bleDiscoveryRefCount++; }
                Monitor.Pulse(_bleDiscoveryThread);
            }

            return new WinBleDiscovery() { };
        }
        */

        protected override object[] GetBleDeviceKeys()
        {
            object notifyObject;
            lock (_notifyThread)
            {
                notifyObject = _bleNotifyObject;
                if (notifyObject == _bleDeviceKeysCacheNotifyObject) { return _bleDeviceKeysCache; }
            }

            var paths = new List<object>();

            if (_bleIsSupported)
            {
                NativeMethods.EnumerateDeviceInterfaces(NativeMethods.GuidForBluetoothLEDevice, (deviceInfoSet, deviceInfoData, deviceInterfaceData, deviceID, devicePath) =>
                    {
                        string friendlyName;
                        if (NativeMethods.TryGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_FRIENDLYNAME, out friendlyName))
                        {

                        }
                        else
                        {
                            friendlyName = null;
                        }

                        if (!string.IsNullOrEmpty(friendlyName))
                        {
                            paths.Add(new BleDevicePath()
                            {
                                DeviceID = deviceID,
                                DevicePath = devicePath,
                                FriendlyName = friendlyName
                            });
                        }
                    });
            }

            var keys = paths.ToArray();
            lock (_notifyThread)
            {
                _bleDeviceKeysCacheNotifyObject = notifyObject;
                _bleDeviceKeysCache = keys;
            }
            return keys;
        }

        protected override object[] GetHidDeviceKeys()
        {
            object notifyObject;
            lock (_notifyThread)
            {
                notifyObject = _hidNotifyObject;
                if (notifyObject == _hidDeviceKeysCacheNotifyObject) { return _hidDeviceKeysCache; }
            }

            var paths = new List<object>();

            var hidGuid = NativeMethods.HidD_GetHidGuid();
            NativeMethods.EnumerateDeviceInterfaces(hidGuid, (_, __, ___, deviceID, devicePath) =>
                {
                    paths.Add(new HidDevicePath()
                    {
                        DeviceID = deviceID,
                        DevicePath = devicePath
                    });
                });

            var keys = paths.ToArray();
            lock (_notifyThread)
            {
                _hidDeviceKeysCacheNotifyObject = notifyObject;
                _hidDeviceKeysCache = keys;
            }
            return keys;
        }

        protected override object[] GetSerialDeviceKeys()
        {
            object notifyObject;
            lock (_notifyThread)
            {
                notifyObject = _serNotifyObject;
                if (notifyObject == _serDeviceKeysCacheNotifyObject) { return _serDeviceKeysCache; }
            }

            var paths = new List<object>();

            NativeMethods.EnumerateDevices(NativeMethods.GuidForPortsClass, (deviceInfoSet, deviceInfoData, deviceID) =>
                {
                    string friendlyName, portName;
                    if (NativeMethods.TryGetSerialPortFriendlyName(deviceInfoSet, ref deviceInfoData, out friendlyName) &&
                        NativeMethods.TryGetSerialPortName(deviceInfoSet, ref deviceInfoData, out portName))
                    {
                        paths.Add(new SerialDevicePath()
                        {
                            DeviceID = deviceID,
                            DevicePath = @"\\.\" + portName,
                            FileSystemName = portName,
                            FriendlyName = friendlyName
                        });
                    }
                });

            var keys = paths.ToArray();
            lock (_notifyThread)
            {
                _serDeviceKeysCacheNotifyObject = notifyObject;
                _serDeviceKeysCache = keys;
            }
            return keys;
        }

        protected override bool TryCreateBleDevice(object key, out Device device)
        {
            var path = (BleDevicePath)key;
            device = WinBleDevice.TryCreate(path.DevicePath, path.DeviceID, path.FriendlyName);
            return device != null;
        }

        protected override bool TryCreateHidDevice(object key, out Device device)
        {
            var path = (HidDevicePath)key;
            device = WinHidDevice.TryCreate(path.DevicePath, path.DeviceID);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            var path = (SerialDevicePath)key;
            device = WinSerialDevice.TryCreate(path.DevicePath, path.FileSystemName, path.FriendlyName); return true;
        }

        public override bool AreDriversBeingInstalled
        {
            get
            {
                try
                {
                    return NativeMethods.WAIT_TIMEOUT == NativeMethods.CMP_WaitNoPendingInstallEvents(0);
                }
                catch
                {
                    return false;
                }
            }
        }

        public override string FriendlyName
        {
            get { return "Windows HID"; }
        }

        public override bool IsSupported
        {
            get { return _isSupported; }
        }
    }
}
