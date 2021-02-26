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
using System.ComponentModel;
using HidSharp.Utility;

namespace HidSharp.Experimental
{
    public abstract class BleStream : DeviceStream
    {
        /// <exclude/>
        protected BleStream(BleDevice device, BleService service)
            : base(device)
        {
            Throw.If.Null(service).False(service.Device == device);
            Service = service;

            ReadTimeout = 3000;
            WriteTimeout = 3000;
        }

        #region Stream
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void Flush()
        {

        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override sealed void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }
        #endregion

        #region Characteristics
        public byte[] ReadCharacteristic(BleCharacteristic characteristic)
        {
            return ReadCharacteristic(characteristic, RequestFlags);
        }

        public abstract byte[] ReadCharacteristic(BleCharacteristic characteristic, BleRequestFlags requestFlags);

        public void WriteCharacteristic(BleCharacteristic characteristic, byte[] value)
        {
            Throw.If.Null(value, "value");
            WriteCharacteristic(characteristic, value, 0, value.Length);
        }

        public void WriteCharacteristic(BleCharacteristic characteristic, byte[] value, int offset, int count)
        {
            WriteCharacteristic(characteristic, value, offset, count, RequestFlags);
        }

        public abstract void WriteCharacteristic(BleCharacteristic characteristic, byte[] value, int offset, int count, BleRequestFlags requestFlags);

        public void WriteCharacteristicWithoutResponse(BleCharacteristic characteristic, byte[] value)
        {
            Throw.If.Null(value, "value");
            WriteCharacteristicWithoutResponse(characteristic, value, 0, value.Length);
        }

        public void WriteCharacteristicWithoutResponse(BleCharacteristic characteristic, byte[] value, int offset, int count)
        {
            WriteCharacteristicWithoutResponse(characteristic, value, offset, count, RequestFlags);
        }

        public abstract void WriteCharacteristicWithoutResponse(BleCharacteristic characteristic, byte[] value, int offset, int count, BleRequestFlags requestFlags);

        public IAsyncResult BeginWriteCharacteristicWithoutResponse(BleCharacteristic characteristic, byte[] value, int offset, int count,
                                                                    AsyncCallback callback, object state)
        {
            return BeginWriteCharacteristicWithoutResponse(characteristic, value, offset, count, RequestFlags, callback, state);
        }

        public virtual IAsyncResult BeginWriteCharacteristicWithoutResponse(BleCharacteristic characteristic, byte[] value, int offset, int count, BleRequestFlags requestFlags,
                                                                            AsyncCallback callback, object state)
        {
            return AsyncResult<int>.BeginOperation(delegate()
            {
                WriteCharacteristicWithoutResponse(characteristic, value, offset, count, requestFlags); return 0;
            }, callback, state);
        }

        public virtual void EndWriteCharacteristicWithoutResponse(IAsyncResult asyncResult)
        {
            AsyncResult<int>.EndOperation(asyncResult);
        }
        #endregion

        #region Characteristic Descriptors
        public byte[] ReadDescriptor(BleDescriptor descriptor)
        {
            return ReadDescriptor(descriptor, RequestFlags);
        }

        public abstract byte[] ReadDescriptor(BleDescriptor descriptor, BleRequestFlags requestFlags);

        public void WriteDescriptor(BleDescriptor descriptor, byte[] value)
        {
            Throw.If.Null(value, "value");
            WriteDescriptor(descriptor, value, 0, value.Length);
        }

        public void WriteDescriptor(BleDescriptor descriptor, byte[] value, int offset, int count)
        {
            WriteDescriptor(descriptor, value, offset, count, RequestFlags);
        }

        public abstract void WriteDescriptor(BleDescriptor descriptor, byte[] value, int offset, int count, BleRequestFlags requestFlags);
        #endregion

        #region Events
        public abstract bool CanReadEventNow();

        public abstract BleEvent ReadEvent();

        public virtual IAsyncResult BeginReadEvent(AsyncCallback callback, object state)
        {
            return AsyncResult<BleEvent>.BeginOperation(delegate()
            {
                return ReadEvent();
            }, callback, state);
        }

        public virtual BleEvent EndReadEvent(IAsyncResult asyncResult)
        {
            return AsyncResult<BleEvent>.EndOperation(asyncResult);
        }
        #endregion

        #region CCCD
        public BleCccd ReadCccd(BleCharacteristic characteristic)
        {
            return ReadCccd(characteristic, RequestFlags);
        }

        public BleCccd ReadCccd(BleCharacteristic characteristic, BleRequestFlags requestFlags)
        {
            BleDescriptor descriptor;
            if (!characteristic.TryGetDescriptor(BleUuids.Cccd, out descriptor))
            {
                HidSharpDiagnostics.Trace("Characteristic {0} does not have a CCCD, so it could not be read.", characteristic);
                return BleCccd.None;
            }

            var value = ReadDescriptor(descriptor, requestFlags); var cccd = BleCccd.None;
            if (value.Length >= 1 && value[0] == (byte)BleCccd.Notification) { cccd = BleCccd.Notification; }
            if (value.Length >= 1 && value[0] == (byte)BleCccd.Indication) { cccd = BleCccd.Indication; }
            return cccd;
        }

        public void WriteCccd(BleCharacteristic characteristic, BleCccd cccd)
        {
            WriteCccd(characteristic, cccd, RequestFlags);
        }

        public void WriteCccd(BleCharacteristic characteristic, BleCccd cccd, BleRequestFlags requestFlags)
        {
            Throw.If.Null(characteristic, "characteristic");

            BleDescriptor descriptor;
            if (!characteristic.TryGetDescriptor(BleUuids.Cccd, out descriptor))
            {
                HidSharpDiagnostics.Trace("Characteristic {0} does not have a CCCD, so {1} could not be written.", characteristic, cccd);
                return;
            }

            if (cccd == BleCccd.Notification)
            {
                HidSharpDiagnostics.PerformStrictCheck(characteristic.IsNotifiable, "Characteristic doesn't support Notify.");
            }

            if (cccd == BleCccd.Indication)
            {
                HidSharpDiagnostics.PerformStrictCheck(characteristic.IsIndicatable, "Characteristic doesn't support Indicate.");
            }


            var value = new byte[2];
            value[0] = (byte)((ushort)cccd >> 0);
            value[1] = (byte)((ushort)cccd >> 8);
            WriteDescriptor(descriptor, value);
        }
        #endregion

        /// <summary>
        /// Gets the <see cref="BleDevice"/> associated with this stream.
        /// </summary>
        public new BleDevice Device
        {
            get { return (BleDevice)base.Device; }
        }

        /// <summary>
        /// Gets the <see cref="BleService"/> associated with this stream.
        /// </summary>
        public BleService Service
        {
            get;
            private set;
        }

        public BleRequestFlags RequestFlags
        {
            get;
            set;
        }
    }
}
