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
using System.IO;

namespace HidSharp
{
    public static class DeviceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IOException"/> class, and associates a <see cref="Device"/> with it.
        /// </summary>
        /// <param name="device">The device that caused the exception.</param>
        /// <param name="message">A description of the error.</param>
        /// <returns>The new <see cref="IOException"/>.</returns>
        public static IOException CreateIOException(Device device, string message)
        {
            return new Exceptions.DeviceIOException(device, message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IOException"/> class, and associates a <see cref="Device"/> with it.
        /// </summary>
        /// <param name="device">The device that caused the exception.</param>
        /// <param name="message">A description of the error.</param>
        /// <param name="hresult">An integer identifying the error that has occurred.</param>
        /// <returns>The new <see cref="IOException"/>.</returns>
        public static IOException CreateIOException(Device device, string message, int hresult)
        {
            return new Exceptions.DeviceIOException(device, message, hresult);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedAccessException"/> class, and associates a <see cref="Device"/> with it.
        /// </summary>
        /// <param name="device">The device that caused the exception.</param>
        /// <param name="message">A description of the error.</param>
        /// <returns>The new <see cref="UnauthorizedAccessException"/>.</returns>
        public static UnauthorizedAccessException CreateUnauthorizedAccessException(Device device, string message)
        {
            return new Exceptions.DeviceUnauthorizedAccessException(device, message);
        }

        /// <summary>
        /// Gets the <see cref="Device"/> associated with the exception, if any.
        /// </summary>
        /// <param name="exception">The exception to get the associated <see cref="Device"/> for.</param>
        /// <returns>The associated <see cref="Device"/>, or null if none is associated with it.</returns>
        public static Device GetDevice(Exception exception)
        {
            var hidException = exception as Exceptions.IDeviceException;
            return hidException != null ? hidException.Device : null;
        }
    }
}
