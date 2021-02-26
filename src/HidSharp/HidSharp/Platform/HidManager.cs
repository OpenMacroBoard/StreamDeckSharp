#region License
/* Copyright 2012-2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Threading;
using HidSharp.Experimental;
using HidSharp.Utility;

namespace HidSharp.Platform
{
    abstract class HidManager
    {
        sealed class DeviceTypeInfo
        {
            public delegate object[] GetDeviceKeysCallbackType();
            public delegate bool TryCreateDeviceCallbackType(object key, out Device device);

            public object DevicesLock = new object();
            public Dictionary<object, Device> DeviceList = new Dictionary<object, Device>();
            public GetDeviceKeysCallbackType GetDeviceKeysCallback;
            public TryCreateDeviceCallbackType TryCreateDeviceCallback;
        }
        DeviceTypeInfo _ble, _hid, _serial;

        protected HidManager()
        {
            _ble = new DeviceTypeInfo()
            {
                GetDeviceKeysCallback = GetBleDeviceKeys,
                TryCreateDeviceCallback = TryCreateBleDevice
            };

            _hid = new DeviceTypeInfo()
            {
                GetDeviceKeysCallback = GetHidDeviceKeys,
                TryCreateDeviceCallback = TryCreateHidDevice
            };

            _serial = new DeviceTypeInfo()
            {
                GetDeviceKeysCallback = GetSerialDeviceKeys,
                TryCreateDeviceCallback = TryCreateSerialDevice
            };
        }

        internal void InitializeEventManager()
        {
            EventManager = CreateEventManager();
            EventManager.Start();
        }

        protected virtual SystemEvents.EventManager CreateEventManager()
        {
            return new SystemEvents.DefaultEventManager();
        }

        protected virtual void Run(Action readyCallback)
        {
            readyCallback();
        }

        internal void RunImpl(object readyEvent)
        {
            Run(() => ((ManualResetEvent)readyEvent).Set());
        }

        protected static void RunAssert(bool condition, string error)
        {
            if (!condition) { throw new InvalidOperationException(error); }
        }

        public virtual BleDiscovery BeginBleDiscovery()
        {
            throw new NotSupportedException();
        }

        IEnumerable<Device> GetDevices(DeviceTypeInfo type)
        {
            var _deviceList = type.DeviceList;
            var devicesLock = type.DevicesLock;
            var getDeviceKeysCallback = type.GetDeviceKeysCallback;
            var tryCreateDeviceCallback = type.TryCreateDeviceCallback;

            Device[] deviceListArray;

            lock (devicesLock)
            {
                object[] devices = getDeviceKeysCallback();
                object[] additions = devices.Except(_deviceList.Keys).ToArray();
                object[] removals = _deviceList.Keys.Except(devices).ToArray();

                if (additions.Length > 0)
                {
                    int completedAdditions = 0;

                    foreach (object addition in additions)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(key =>
                        {
                            Device device;
                            bool created = tryCreateDeviceCallback(key, out device);

                            if (created)
                            {
                                // By not adding on failure, we'll end up retrying every time.
                                lock (_deviceList)
                                {
                                    _deviceList.Add(key, device);
                                    HidSharpDiagnostics.Trace("Detected a new device: {0}", key);
                                }
                            }

                            lock (_deviceList)
                            {
                                completedAdditions++; Monitor.Pulse(_deviceList);
                            }
                        }), addition);
                    }

                    lock (_deviceList)
                    {
                        while (completedAdditions != additions.Length) { Monitor.Wait(_deviceList); }
                    }
                }

                foreach (object key in removals)
                {
                    _deviceList.Remove(key);
                    HidSharpDiagnostics.Trace("Detected a device removal: {0}", key);
                }
                deviceListArray = _deviceList.Values.ToArray();
            }

            return deviceListArray;
        }

        public IEnumerable<Device> GetDevices(DeviceTypes types)
        {
            var devices = Enumerable.Empty<Device>();
            if (0 != (types & DeviceTypes.Hid)) { devices = devices.Concat(GetDevices(_hid)); }
            if (0 != (types & DeviceTypes.Serial)) { devices = devices.Concat(GetDevices(_serial)); }
            if (0 != (types & DeviceTypes.Ble)) { devices = devices.Concat(GetDevices(_ble)); }
            return devices;
        }

        protected abstract object[] GetBleDeviceKeys();

        protected abstract object[] GetHidDeviceKeys();

        protected abstract object[] GetSerialDeviceKeys();

        protected abstract bool TryCreateBleDevice(object key, out Device device);

        protected abstract bool TryCreateHidDevice(object key, out Device device);

        protected abstract bool TryCreateSerialDevice(object key, out Device device);

        public virtual bool AreDriversBeingInstalled
        {
            get { return false; }
        }

        public SystemEvents.EventManager EventManager
        {
            get;
            private set;
        }

        public abstract string FriendlyName
        {
            get;
        }

        public abstract bool IsSupported
        {
            get;
        }
    }
}
