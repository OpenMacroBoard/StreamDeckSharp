#region License
/* Copyright 2012 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Platform.Unsupported
{
    sealed class UnsupportedHidManager : HidManager
    {
        protected override object[] GetBleDeviceKeys()
        {
            return new object[0];
        }

        protected override object[] GetHidDeviceKeys()
        {
            return new object[0];
        }

        protected override object[] GetSerialDeviceKeys()
        {
            return new object[0];
        }

        protected override bool TryCreateBleDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }

        protected override bool TryCreateHidDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            throw new NotImplementedException();
        }

        public override string FriendlyName
        {
            get { return "Platform Not Supported"; }
        }

        public override bool IsSupported
        {
            get { return true; }
        }
    }
}
