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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HidSharp.Experimental
{
    /// <summary>
    /// Represents a Bluetooth Low Energy device.
    /// </summary>
    [ComVisible(true), Guid("A7AEE7B8-893D-41B6-84F7-6BDA4EE3AA3F")]
    public abstract class BleDevice : Device
    {
        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new BleStream Open()
        {
            return (BleStream)base.Open();
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new BleStream Open(OpenConfiguration openConfig)
        {
            return (BleStream)base.Open(openConfig);
        }

        public BleStream Open(BleService service)
        {
            return Open(service, new OpenConfiguration());
        }

        public BleStream Open(BleService service, OpenConfiguration openConfig)
        {
            Throw.If.Null(service).Null(openConfig);

            openConfig = openConfig.Clone();
            openConfig.SetOption(OpenOption.BleService, service);
            return Open(openConfig);
        }

        /*
        public abstract bool GetConnectionState();
        */

        public abstract BleService[] GetServices();

        public BleService GetServiceOrNull(BleUuid uuid)
        {
            BleService service;
            return TryGetService(uuid, out service) ? service : null;
        }

        public virtual bool HasService(BleUuid uuid)
        {
            BleService service;
            return TryGetService(uuid, out service);
        }

        public virtual bool TryGetService(BleUuid uuid, out BleService service)
        {
            foreach (var s in GetServices())
            {
                if (s.Uuid == uuid) { service = s; return true; }
            }

            service = null; return false;
        }

        public override bool HasImplementationDetail(Guid detail)
        {
            return base.HasImplementationDetail(detail) || detail == ImplementationDetail.BleDevice;
        }

        public override string ToString()
        {
            string friendlyName = "(unknown friendly name)";
            try { friendlyName = GetFriendlyName(); }
            catch { }

            return friendlyName;
        }
    }
}
