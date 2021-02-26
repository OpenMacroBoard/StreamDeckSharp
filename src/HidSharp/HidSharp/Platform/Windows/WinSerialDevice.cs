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

namespace HidSharp.Platform.Windows
{
    sealed class WinSerialDevice : SerialDevice
    {
        string _path;
        string _fileSystemName;
        string _friendlyName;

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            var stream = new WinSerialStream(this);
            stream.Init(DevicePath);
            return stream;
        }

        internal static WinSerialDevice TryCreate(string portName, string fileSystemName, string friendlyName)
        {
            return new WinSerialDevice() { _path = portName, _fileSystemName = fileSystemName, _friendlyName = friendlyName };
        }

        public override string GetFileSystemName()
        {
            return _fileSystemName;
        }

        public override string GetFriendlyName()
        {
            return _friendlyName;
        }

        public override string DevicePath
        {
            get { return _path; }
        }
    }
}
