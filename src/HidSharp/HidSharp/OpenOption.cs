#region License
/* Copyright 2016-2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp
{
    public delegate object OpenOptionDeserializeCallback(byte[] buffer);
    public delegate byte[] OpenOptionSerializeCallback(object value);

    /// <summary>
    /// Options for opening a device stream.
    /// </summary>
    public sealed class OpenOption
    {
        static Dictionary<Guid, OpenOption> _options;

        /// <summary>
        /// Use HIDSharp's exclusivity layer.
        /// This allows one process using HIDSharp to lock other processes out of using a HID device.
        /// Processes may request interruption, allowing interprocess cooperation.
        /// (For example, a data logging application can make itself interruptible and allow another process to use the HID device temporarily.)
        /// 
        /// Defaults to <c>false</c>.
        /// </summary>
        public static OpenOption Exclusive { get; private set; }

        /// <summary>
        /// Allow other processes to send interruption requests.
        /// If another other process with higher priority attempts to open the HID device this process is using,
        /// this process will receive an <see cref="DeviceStream.InterruptRequested"/> event on an arbitrary thread.
        /// 
        /// <see cref="OpenOption.Exclusive"/> must be <c>true</c> for this to work.
        /// Defaults to <c>false</c>.
        /// </summary>
        public static OpenOption Interruptible { get; private set; }

        /// <summary>
        /// The priority of the process. This is used for interruption requests.
        /// <see cref="OpenOption.Exclusive"/> must be <c>true</c> for this to work.
        /// Defaults to <see cref="OpenPriority.Normal"/>.
        /// </summary>
        public static OpenOption Priority { get; private set; }

        /// <summary>
        /// The amount of time to wait for an interruptible process to give up the HID device before failing to open the stream.
        /// Defaults to 3000 milliseconds.
        /// </summary>
        public static OpenOption TimeoutIfInterruptible { get; private set; }

        /// <summary>
        /// The amount of time to wait for a transient process to give up the HID device before failing to open the stream.
        /// Defaults to 30000 milliseconds.
        /// </summary>
        public static OpenOption TimeoutIfTransient { get; private set; }

        /// <summary>
        /// If a HID device is opened by another process transiently, HIDSharp will wait some time for the process to give up the HID device before failing to open the stream.
        /// 
        /// <see cref="OpenOption.Exclusive"/> must be <c>true</c> for this to work.
        /// Defaults to <c>false</c>.
        /// </summary>
        public static OpenOption Transient { get; private set; }

        internal static OpenOption BleService { get; private set; }

        OpenOptionDeserializeCallback _deserializeCallback;
        OpenOptionSerializeCallback _serializeCallback;

        static OpenOption()
        {
            _options = new Dictionary<Guid, OpenOption>();

            Exclusive = OpenOption.New(new Guid("{49DB23CD-727E-4788-BBAD-7D67ACCBC469}"),
                                       deserializeCallback: DeserializeBoolean,
                                       serializeCallback: SerializeBoolean,
                                       defaultValue: false,
                                       friendlyName: "Exclusive");
            Interruptible = OpenOption.New(new Guid("{55C9673C-A49C-4190-B0BC-294020EAAE54}"),
                                           deserializeCallback: DeserializeBoolean,
                                           serializeCallback: SerializeBoolean,
                                           defaultValue: false,
                                           friendlyName: "Interruptible");
            Priority = OpenOption.New(new Guid("{3C065A90-A685-44BD-BE06-50EDACF51F11}"),
                                      deserializeCallback: buffer =>
                                      {
                                          if (buffer.Length < 1) { return null; }
                                          return (object)(OpenPriority)buffer[0];
                                      },
                                      serializeCallback: value =>
                                      {
                                          return new[] { (byte)(OpenPriority)value };
                                      },
                                      defaultValue: OpenPriority.Normal,
                                      friendlyName: "Priority");
            TimeoutIfInterruptible = OpenOption.New(new Guid("{C8F9B70B-302F-4326-B28D-E823C4E6131E}"),
                                                    deserializeCallback: DeserializeInt32,
                                                    serializeCallback: SerializeInt32,
                                                    defaultValue: 3000,
                                                    friendlyName: "TimeoutIfInterruptible");
            TimeoutIfTransient = OpenOption.New(new Guid("{0A918B9F-6FF5-4A14-A945-78685B37BF40}"),
                                                deserializeCallback: DeserializeInt32,
                                                serializeCallback: SerializeInt32,
                                                defaultValue: 30000,
                                                friendlyName: "TimeoutIfTransient");
            Transient = OpenOption.New(new Guid("{C564DE4B-A9A8-4F5F-A7E4-1A14AF9BEFEC}"),
                                       deserializeCallback: DeserializeBoolean,
                                       serializeCallback: SerializeBoolean,
                                       defaultValue: false,
                                       friendlyName: "Transient");
            BleService = OpenOption.New(new Guid("{A0E7B2C1-656D-40FB-9C29-3CD28F54D45D}"),
                                        deserializeCallback: _ => { throw new NotImplementedException(); },
                                        serializeCallback: _ => { throw new NotImplementedException(); },
                                        defaultValue: null,
                                        friendlyName: "BLE Service");
        }

        OpenOption()
        {

        }

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override string ToString()
        {
            return FriendlyName;
        }

        public static OpenOption FromGuid(Guid guid)
        {
            lock (_options)
            {
                OpenOption option;
                return _options.TryGetValue(guid, out option) ? option : null;
            }
        }

        public static OpenOption New(Guid guid,
                                     OpenOptionDeserializeCallback deserializeCallback,
                                     OpenOptionSerializeCallback serializeCallback,
                                     object defaultValue = null, string friendlyName = null)
        {
            Throw.If.Null(deserializeCallback, "deserializeCallback");
            Throw.If.Null(serializeCallback, "serializeCallback");

            OpenOption option;
            lock (_options)
            {
                if (_options.ContainsKey(guid)) { throw new ArgumentException(); }

                option = new OpenOption()
                {
                    Guid = guid,
                    DefaultValue = defaultValue,
                    FriendlyName = friendlyName ?? guid.ToString("B"),
                    _deserializeCallback = deserializeCallback,
                    _serializeCallback = serializeCallback
                };
                _options.Add(guid, option);
            }
            return option;
        }

        static object DeserializeBoolean(byte[] buffer)
        {
            if (buffer.Length < 1) { return null; }
            return (buffer[0] & 1) != 0;
        }

        static byte[] SerializeBoolean(object value)
        {
            return new[] { (byte)((bool)value ? 1 : 0) };
        }

        static object DeserializeInt32(byte[] buffer)
        {
            if (buffer.Length < 4) { return null; }
            return BitConverter.ToInt32(buffer, 0);
        }

        static byte[] SerializeInt32(object value)
        {
            return BitConverter.GetBytes((int)value);
        }

        public object Deserialize(byte[] buffer)
        {
            return _deserializeCallback(buffer);
        }

        public byte[] Serialize(object value)
        {
            return _serializeCallback(value);
        }

        public object DefaultValue
        {
            get;
            private set;
        }

        public string FriendlyName
        {
            get;
            private set;
        }

        public Guid Guid
        {
            get;
            private set;
        }
    }
}
