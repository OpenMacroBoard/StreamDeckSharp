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
using System.Diagnostics;
using System.Runtime.InteropServices;
using HidSharp.Experimental;
using HidSharp.Utility;

namespace HidSharp.Platform.Windows
{
    sealed class WinBleStream : SysBleStream
    {
        const int WatchQueueLimit = 100;
        object _readSync = new object(), _writeSync = new object(), _watchSync = new object();
        IntPtr _handle, _closeEventHandle;
        IntPtr _watchRegisterEventHandle; // FIXME: What is the correct erorr-state value here? (IntPtr)(-1) or IntPtr.Zero?
        IntPtr _watchEventHandle;         // For notifying.
        NativeMethods.BLUETOOTH_GATT_EVENT_CALLBACK _watchCallback;
        Queue<BleEvent> _watchEvents;
        Dictionary<ushort, BleCharacteristic> _watchMap;

        internal WinBleStream(WinBleDevice device, WinBleService service)
            : base(device, service)
        {
            _closeEventHandle = NativeMethods.CreateManualResetEventOrThrow();
            _watchEventHandle = NativeMethods.CreateManualResetEventOrThrow();
            _watchEvents = new Queue<BleEvent>();
            _watchMap = new Dictionary<ushort, BleCharacteristic>();
        }

        ~WinBleStream()
        {
            Close();
            NativeMethods.CloseHandle(_closeEventHandle);
        }

        internal unsafe void Init(string path)
        {
            IntPtr handle = NativeMethods.CreateFileFromDevice(path, NativeMethods.EFileAccess.Read | NativeMethods.EFileAccess.Write, 0);
            if (handle == (IntPtr)(-1))
            {
                throw DeviceException.CreateIOException(Device, "Unable to open BLE service (" + path + ").");
            }

            _handle = handle;
            HandleInitAndOpen();

            // Let's register to watch all possible characteristics.
            var watchedCharacteristics = new List<WinBleCharacteristic>();

            foreach (var characteristic in Service.GetCharacteristics())
            {
                if (characteristic.IsNotifiable || characteristic.IsIndicatable)
                {
                    watchedCharacteristics.Add((WinBleCharacteristic)characteristic);
                }
            }

            if (watchedCharacteristics.Count > 0)
            {
                var eb = stackalloc byte[checked(NativeMethods.BLUETOOTH_GATT_VALUE_CHANGED_EVENT_REGISTRATION.Size +
                                            NativeMethods.BTH_LE_GATT_CHARACTERISTIC.Size * watchedCharacteristics.Count)];

                var er = (NativeMethods.BLUETOOTH_GATT_VALUE_CHANGED_EVENT_REGISTRATION*)eb;
                er->NumCharacteristics = (ushort)watchedCharacteristics.Count;
                eb += NativeMethods.BLUETOOTH_GATT_VALUE_CHANGED_EVENT_REGISTRATION.Size;

                for (int i = 0; i < watchedCharacteristics.Count; i++)
                {
                    var wc = watchedCharacteristics[i];
                    Marshal.StructureToPtr(wc.NativeData, (IntPtr)eb,false);
                    eb += NativeMethods.BTH_LE_GATT_CHARACTERISTIC.Size;

                    _watchMap[wc.NativeData.AttributeHandle] = wc;
                }

                _watchCallback = new NativeMethods.BLUETOOTH_GATT_EVENT_CALLBACK(EventCallback);
                int error = NativeMethods.BluetoothGATTRegisterEvent(handle, NativeMethods.BTH_LE_GATT_EVENT_TYPE.CharacteristicValueChangedEvent,
                                                                     er, _watchCallback, IntPtr.Zero, out _watchRegisterEventHandle);
                if (error != 0) { Debug.Assert(error == 0); _watchRegisterEventHandle = IntPtr.Zero; }
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (!HandleClose()) { return; }

            if (_watchRegisterEventHandle != IntPtr.Zero)
            {
                int error = NativeMethods.BluetoothGATTUnregisterEvent(_watchRegisterEventHandle);
                Debug.Assert(error == 0);
            }

            NativeMethods.SetEvent(_closeEventHandle);
            HandleRelease();

            base.Dispose(disposing);
        }

        internal override void HandleFree()
        {
            NativeMethods.CloseHandle(ref _handle);
            NativeMethods.CloseHandle(ref _closeEventHandle);
            NativeMethods.CloseHandle(ref _watchEventHandle);
        }

        #region Characteristics
        public override unsafe byte[] ReadCharacteristic(BleCharacteristic characteristic, BleRequestFlags requestFlags)
        {
            Throw.If.Null(characteristic, "characteristic");
            HidSharpDiagnostics.PerformStrictCheck(characteristic.IsReadable, "Characteristic doesn't support Read.");

            var flags = GetGattFlags(requestFlags);

            HandleAcquireIfOpenOrFail();
            try
            {
                lock (_readSync)
                {
                    int error;
                    var wc = (WinBleCharacteristic)characteristic;

                    ushort valueSize;
                    error = NativeMethods.BluetoothGATTGetCharacteristicValue(_handle,
                                                                              ref wc.NativeData,
                                                                              0, null,
                                                                              out valueSize,
                                                                              flags | ((requestFlags & BleRequestFlags.Cacheable) == 0 ? NativeMethods.BLUETOOTH_GATT_FLAGS.FORCE_READ_FROM_DEVICE : 0));
                    if (error != NativeMethods.ERROR_MORE_DATA || valueSize < NativeMethods.BTH_LE_GATT_CHARACTERISTIC_VALUE.Size)
                    {
                        var message = string.Format("Failed to read characteristic {0}.", characteristic);
                        throw DeviceException.CreateIOException(Device, message, error);
                    }

                    var cb = stackalloc byte[valueSize];
                    var cv = (NativeMethods.BTH_LE_GATT_CHARACTERISTIC_VALUE*)cb;

                    ushort valueSize2;
                    error = NativeMethods.BluetoothGATTGetCharacteristicValue(_handle,
                                                                              ref wc.NativeData,
                                                                              valueSize,
                                                                              cv,
                                                                              out valueSize2,
                                                                              flags);
                    if (error != 0 || valueSize != valueSize2 || cv->DataSize > valueSize - NativeMethods.BTH_LE_GATT_CHARACTERISTIC_VALUE.Size)
                    {
                        var message = string.Format("Failed to read characteristic {0}.", characteristic);
                        throw DeviceException.CreateIOException(Device, message, error);
                    }

                    var bytes = new byte[cv->DataSize];
                    Marshal.Copy((IntPtr)(void*)&cv->Data[0], bytes, 0, checked((int)cv->DataSize));
                    return bytes;
                }
            }
            finally
            {
                HandleRelease();
            }
        }

        public override void WriteCharacteristic(BleCharacteristic characteristic, byte[] value, int offset, int count, BleRequestFlags requestFlags)
        {
            Throw.If.Null(characteristic, "characteristic");
            HidSharpDiagnostics.PerformStrictCheck(characteristic.IsWritable, "Characteristic doesn't support Write.");

            var flags = GetGattFlags(requestFlags);
            WriteCharacteristic(characteristic, value, offset, count, flags);
        }

        public override void WriteCharacteristicWithoutResponse(BleCharacteristic characteristic, byte[] value, int offset, int count, BleRequestFlags requestFlags)
        {
            Throw.If.Null(characteristic, "characteristic");
            HidSharpDiagnostics.PerformStrictCheck(characteristic.IsWritableWithoutResponse, "Characteristic doesn't support Write Without Response.");

            var flags = GetGattFlags(requestFlags);
            WriteCharacteristic(characteristic, value, offset, count, flags | NativeMethods.BLUETOOTH_GATT_FLAGS.WRITE_WITHOUT_RESPONSE);
        }

        unsafe void WriteCharacteristic(BleCharacteristic characteristic, byte[] value, int offset, int count, NativeMethods.BLUETOOTH_GATT_FLAGS flags)
        {
            Throw.If.Null(characteristic, "characteristic").Null(value, "value").OutOfRange(value, offset, count);

			HandleAcquireIfOpenOrFail();
            try
            {
                lock (_writeSync)
                {
                    int error;
                    var wc = (WinBleCharacteristic)characteristic;

                    var cb = stackalloc byte[NativeMethods.BTH_LE_GATT_CHARACTERISTIC_VALUE.Size + count];
                    var cv = (NativeMethods.BTH_LE_GATT_CHARACTERISTIC_VALUE*)cb;
                    cv->DataSize = (uint)count; Marshal.Copy(value, offset, (IntPtr)(void*)&cv->Data[0], count);

                    error = NativeMethods.BluetoothGATTSetCharacteristicValue(_handle, ref wc.NativeData, cv, 0, flags);
                    if (error != 0)
                    {
                        var message = string.Format("Failed to write {0} bytes to characteristic {1}.", count, characteristic);
                        throw DeviceException.CreateIOException(Device, message, error);
                    }
                }
            }
            finally
            {
                HandleRelease();
            }
        }
        #endregion

        #region Characteristic Descriptors
        public override unsafe byte[] ReadDescriptor(BleDescriptor descriptor, BleRequestFlags requestFlags)
        {
            Throw.If.Null(descriptor, "descriptor");
            var flags = GetGattFlags(requestFlags);

            HandleAcquireIfOpenOrFail();
            try
            {
                lock (_readSync)
                {
                    int error;
                    var wd = (WinBleDescriptor)descriptor;

                    ushort valueSize;
                    error = NativeMethods.BluetoothGATTGetDescriptorValue(_handle,
                                                                          ref wd.NativeData,
                                                                          0, null,
                                                                          out valueSize,
                                                                          flags | ((requestFlags & BleRequestFlags.Cacheable) == 0 ? NativeMethods.BLUETOOTH_GATT_FLAGS.FORCE_READ_FROM_DEVICE : 0));
                    if (error != NativeMethods.ERROR_MORE_DATA || valueSize < NativeMethods.BTH_LE_GATT_DESCRIPTOR_VALUE.Size)
                    {
                        var message = string.Format("Failed to read descriptor {0}.", descriptor);
                        throw DeviceException.CreateIOException(Device, message, error);
                    }

                    var cb = stackalloc byte[valueSize];
                    var cv = (NativeMethods.BTH_LE_GATT_DESCRIPTOR_VALUE*)cb;

                    ushort valueSize2;
                    error = NativeMethods.BluetoothGATTGetDescriptorValue(_handle,
                                                                          ref wd.NativeData,
                                                                          valueSize,
                                                                          cv,
                                                                          out valueSize2,
                                                                          flags);
                    if (error != 0 || valueSize != valueSize2 || cv->Value.DataSize > valueSize - NativeMethods.BTH_LE_GATT_DESCRIPTOR_VALUE.Size)
                    {
                        var message = string.Format("Failed to read descriptor {0}.", descriptor);
                        throw DeviceException.CreateIOException(Device, message, error);
                    }

                    byte[] data;
                    switch (cv->DescriptorType)
                    {
                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.CharacteristicExtendedProperties:
                            data = new byte[2];
                            if (0 != cv->Params.ExtendedProperties.IsReliableWriteEnabled) { data[0] |= 1; }
                            if (0 != cv->Params.ExtendedProperties.IsAuxiliariesWritable) { data[0] |= 2; }
                            break;

                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.ClientCharacteristicConfiguration:
                            data = new byte[2];
                            if (0 != cv->Params.Cccd.IsSubscribeToNotification) { data[0] |= 1; }
                            if (0 != cv->Params.Cccd.IsSubscribeToIndication) { data[0] |= 2; }
                            break;

                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.ServerCharacteristicConfiguration:
                            data = new byte[2];
                            if (0 != cv->Params.Sccd.IsBroadcast) { data[0] |= 1; }
                            break;

                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.CharacteristicFormat:
                            throw new NotImplementedException();

                        default:
                            //Console.WriteLine(string.Format("{0} {1} {2}", valueSize, cv->DescriptorType, cv->Value.DataSize));
                            data = new byte[cv->Value.DataSize];
                            Marshal.Copy((IntPtr)(void*)&cv->Value.Data[0], data, 0, data.Length);
                            break;
                    }

                    return data;
                }
            }
            finally
            {
                HandleRelease();
            }
        }

        public override unsafe void WriteDescriptor(BleDescriptor descriptor, byte[] value, int offset, int count, BleRequestFlags requestFlags)
        {
            Throw.If.Null(descriptor, "descriptor").Null(value, "value").OutOfRange(value, offset, count);
            var flags = GetGattFlags(requestFlags);

            HandleAcquireIfOpenOrFail();
            try
            {
                lock (_writeSync)
                {
                    int error;
                    var wd = (WinBleDescriptor)descriptor;

                    var dvp = new NativeMethods.BTH_LE_GATT_DESCRIPTOR_VALUE_PARAMS(); byte[] data = null; int dataOffset = 0, dataCount = 0;

                    switch (wd.NativeData.DescriptorType)
                    {
                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.CharacteristicExtendedProperties:
                            dvp.ExtendedProperties.IsReliableWriteEnabled = (byte)(value.Length >= 1 && 0 != (value[offset] & 1) ? 1 : 0);
                            dvp.ExtendedProperties.IsAuxiliariesWritable = (byte)(value.Length >= 1 && 0 != (value[offset] & 2) ? 1 : 0);
                            data = new byte[0]; break;

                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.ClientCharacteristicConfiguration:
                            dvp.Cccd.IsSubscribeToNotification = (byte)(value.Length >= 1 && 0 != (value[offset] & 1) ? 1 : 0);
                            dvp.Cccd.IsSubscribeToIndication = (byte)(value.Length >= 1 && 0 != (value[offset] & 2) ? 1 : 0);
                            data = new byte[0]; break;

                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.ServerCharacteristicConfiguration:
                            dvp.Sccd.IsBroadcast = (byte)(value.Length >= 1 && 0 != (value[offset] & 1) ? 1 : 0);
                            data = new byte[0]; break;

                        case NativeMethods.BTH_LE_GATT_DESCRIPTOR_TYPE.CharacteristicFormat:
                            throw new NotImplementedException();

                        default:
                            data = value; dataOffset = offset; dataCount = count; break;
                    }

                    var db = stackalloc byte[NativeMethods.BTH_LE_GATT_DESCRIPTOR_VALUE.Size + dataCount];
                    var dv = (NativeMethods.BTH_LE_GATT_DESCRIPTOR_VALUE*)db;
                    dv->DescriptorType = wd.NativeData.DescriptorType;
                    dv->DescriptorUuid = wd.NativeData.DescriptorUuid;
                    dv->Params = dvp;
                    dv->Value.DataSize = (uint)dataCount;
                    if (data != null) { Marshal.Copy(data, dataOffset, (IntPtr)(void*)&dv->Value.Data[0], dataCount); }

                    error = NativeMethods.BluetoothGATTSetDescriptorValue(_handle, ref wd.NativeData, dv, flags);
                    if (error != 0)
                    {
                        var message = string.Format("Failed to write {0} bytes to descriptor {1}.", count, descriptor);
                        throw DeviceException.CreateIOException(Device, message, error);
                    }
                }
            }
            finally
            {
                HandleRelease();
            }
        }
        #endregion

        #region Events
        unsafe void EventCallback(NativeMethods.BTH_LE_GATT_EVENT_TYPE eventType,
                                  NativeMethods.BLUETOOTH_GATT_VALUE_CHANGED_EVENT* eventParameter,
                                  IntPtr context)
        {
            if (eventType == NativeMethods.BTH_LE_GATT_EVENT_TYPE.CharacteristicValueChangedEvent)
            {
                BleCharacteristic characteristic;
                if (_watchMap.TryGetValue(eventParameter->ChangedAttributeHandle, out characteristic))
                {
                    if (eventParameter->CharacteristicValueDataSize == (UIntPtr)eventParameter->CharacteristicValue->DataSize)
                    {
                        var value = new byte[eventParameter->CharacteristicValue->DataSize];
                        Marshal.Copy((IntPtr)(void*)eventParameter->CharacteristicValue->Data, value, 0, value.Length);

                        var @event = new BleEvent(characteristic, value);

                        if (HandleAcquire())
                        {
                            try
                            {
                                lock (_watchSync)
                                {
                                    // Drop the oldest events.
                                    while (_watchEvents.Count >= WatchQueueLimit) { _watchEvents.Dequeue(); }

                                    // Queue up the new event.
                                    _watchEvents.Enqueue(@event);
                                    NativeMethods.SetEvent(_watchEventHandle);
                                }
                            }
                            finally
                            {
                                HandleRelease();
                            }
                        }
                    }
                }
            }
        }

        public override bool CanReadEventNow()
        {
            lock (_watchSync)
            {
                return _watchEvents.Count > 0;
            }
        }

        public override unsafe BleEvent ReadEvent()
        {
			HandleAcquireIfOpenOrFail();
            try
            {
                while (true)
                {
                    IntPtr* handles = stackalloc IntPtr[2];
                    handles[0] = _watchEventHandle; handles[1] = _closeEventHandle;
                    uint waitResult = NativeMethods.WaitForMultipleObjects(2, handles, false, NativeMethods.WaitForMultipleObjectsGetTimeout(ReadTimeout));
                    switch (waitResult)
                    {
                        case NativeMethods.WAIT_OBJECT_0: break;
                        case NativeMethods.WAIT_OBJECT_1: throw CommonException.CreateClosedException();
                        default: throw new TimeoutException();
                    }

                    lock (_watchSync)
                    {
                        // We *may* have multiple threads calling ReadEvent(). Another thread may have read the event.
                        // FIXME: If so, the timeout is not going to operate correctly here.
                        if (_watchEvents.Count == 0)
                        {
                            NativeMethods.ResetEvent(_watchEventHandle);
                            continue;
                        }

                        var @event = _watchEvents.Dequeue();
                        if (_watchEvents.Count == 0) { NativeMethods.ResetEvent(_watchEventHandle); }
                        return @event;
                    }
                }
            }
            finally
            {
                HandleRelease();
            }
        }
        #endregion

        static NativeMethods.BLUETOOTH_GATT_FLAGS GetGattFlags(BleRequestFlags requestFlags)
        {
            NativeMethods.BLUETOOTH_GATT_FLAGS flags = 0;
            if (0 != (requestFlags & BleRequestFlags.Authenticated)) { flags |= NativeMethods.BLUETOOTH_GATT_FLAGS.AUTHENTICATED; }
            if (0 != (requestFlags & BleRequestFlags.Encrypted)) { flags |= NativeMethods.BLUETOOTH_GATT_FLAGS.ENCRYPTED; }
            return flags;
        }

        public sealed override int ReadTimeout
        {
            get;
            set;
        }

        public sealed override int WriteTimeout
        {
            get;
            set;
        }
    }
}
