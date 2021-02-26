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
using System.Diagnostics;
using HidSharp.Utility;

namespace HidSharp
{
    public abstract class Device
    {
        /// <summary>
        /// Makes a connection to the device, or throws an exception if the connection cannot be made.
        /// </summary>
        /// <returns>The stream to use to communicate with the device.</returns>
        public DeviceStream Open()
        {
            return Open(null);
        }

        public DeviceStream Open(OpenConfiguration openConfig)
        {
            return OpenDeviceAndRestrictAccess(openConfig ?? new OpenConfiguration());
        }

        protected virtual DeviceStream OpenDeviceAndRestrictAccess(OpenConfiguration openConfig)
        {
            bool exclusive = (bool)openConfig.GetOption(OpenOption.Exclusive);

            DeviceOpenUtility openUtility = null;
            if (exclusive)
            {
                string streamPath = GetStreamPath(openConfig);
                openUtility = new DeviceOpenUtility(this, streamPath, openConfig);
                openUtility.Open();
            }

            DeviceStream stream;
            try
            {
                stream = OpenDeviceDirectly(openConfig);
                if (exclusive)
                {
                    stream.Closed += (sender, e) => openUtility.Close();
                    openUtility.InterruptRequested += (sender, e) =>
                        {
                            stream.OnInterruptRequested();
                            HidSharpDiagnostics.Trace("Delivered an interrupt request.");
                        };
                }
            }
            catch
            {
                if (exclusive) { openUtility.Close(); }
                throw;
            }

            return stream;
        }

        protected abstract DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig);

        // Used for exclusion... and also may be used inside OpenDeviceDirectly if desired.
        protected virtual string GetStreamPath(OpenConfiguration openConfig)
        {
            return DevicePath;
        }

        /// <summary>
        /// Tries to make a connection to the device.
        /// </summary>
        /// <param name="stream">The stream to use to communicate with the device.</param>
        /// <returns><c>true</c> if the connection was successful.</returns>
        public bool TryOpen(out DeviceStream stream)
        {
            return TryOpen(null, out stream);
        }

        public bool TryOpen(OpenConfiguration openConfig, out DeviceStream stream)
        {
            Exception exception;
            return TryOpen(openConfig, out stream, out exception);
        }

        public bool TryOpen(OpenConfiguration openConfig, out DeviceStream stream, out Exception exception)
        {
            try
            {
                stream = Open(openConfig); exception = null; return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                stream = null; exception = e; return false;
            }
        }

        /// <summary>
        /// Returns the file system path of the device.
        /// This can be used to check permissions on Linux hidraw, for instance.
        /// </summary>
        /// <returns>The file system path.</returns>
        public abstract string GetFileSystemName();

        /// <summary>
        /// Returns a name appropriate for display.
        /// </summary>
        /// <returns>The friendly name.</returns>
        public abstract string GetFriendlyName();

        /// <summary>
        /// Checks if a particular implementation detail, such as the use of the Linux hidraw API, applies to this device.
        /// See <see cref="ImplementationDetail"/> for a list of possible details.
        /// </summary>
        /// <param name="detail">The detail to check.</param>
        /// <returns><c>true</c> if the implementation detail applies.</returns>
        public virtual bool HasImplementationDetail(Guid detail)
        {
            return false;
        }

        /// <summary>
        /// The operating system's name for the device.
        /// 
        /// If you have multiple devices with the same Vendor ID, Product ID, Serial Number, etc.,
        /// this may be useful for differentiating them.
        /// </summary>
        public abstract string DevicePath
        {
            get;
        }
    }
}
