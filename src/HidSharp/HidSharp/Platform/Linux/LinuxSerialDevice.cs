#region License
/* Copyright 2017-2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Platform.Linux
{
    sealed class LinuxSerialDevice : SerialDevice
    {
        string _portName;

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            return new LinuxSerialStream(this);
        }

        internal static LinuxSerialDevice TryCreate(string portName)
        {
            return new LinuxSerialDevice() { _portName = portName };
        }

        public override string GetFileSystemName()
        {
            return _portName;
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.Linux;
        }

        public override string DevicePath
        {
            get { return _portName; }
        }
    }
}
