#region License
/* Copyright 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
    sealed class MacSerialDevice : SerialDevice
    {
        NativeMethods.io_string_t _path;
        string _fileSystemName;

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            return new MacSerialStream(this);
        }

        internal static MacSerialDevice TryCreate(NativeMethods.io_string_t path)
        {
            var d = new MacSerialDevice() { _path = path };

            var handle = NativeMethods.IORegistryEntryFromPath(0, ref path).ToIOObject();
            if (!handle.IsSet) { return null; }

            using (handle)
            {
                d._fileSystemName = NativeMethods.IORegistryEntryGetCFProperty_String(handle, NativeMethods.kIOCalloutDeviceKey);
                if (d._fileSystemName == null) { return null; }
            }

            return d;
        }

        public override string GetFileSystemName()
        {
            return _fileSystemName;
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.MacOS;
        }

        public override string DevicePath
        {
            get { return _path.ToString(); }
        }
    }
}
