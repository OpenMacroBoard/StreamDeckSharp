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

namespace HidSharp
{
    /// <summary>
    /// Represents a serial device.
    /// </summary>
    public abstract class SerialDevice : Device
    {
        /// <inheritdoc/>
        public new SerialStream Open()
        {
            return Open(null);
        }

        /// <inheritdoc/>
        public new SerialStream Open(OpenConfiguration openConfig)
        {
            return (SerialStream)base.Open(openConfig);
        }

        /// <inheritdoc/>
        public bool TryOpen(out SerialStream stream)
        {
            return TryOpen(null, out stream);
        }

        /// <inheritdoc/>
        public bool TryOpen(OpenConfiguration openConfig, out SerialStream stream)
        {
            DeviceStream baseStream;
            bool result = base.TryOpen(openConfig, out baseStream);
            stream = (SerialStream)baseStream; return result;
        }

        /// <inheritdoc/>
        public override string GetFriendlyName()
        {
            return GetFileSystemName();
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.SerialDevice;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string fileSystemName = "(unknown filesystem name)";
            try { fileSystemName = GetFileSystemName(); } catch { }

            return string.Format("{0} ({1})", fileSystemName, DevicePath);
        }
    }
}
