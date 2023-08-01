using StreamDeckSharp.Internals;
using System;

namespace StreamDeckSharp.Tests
{
    internal static class GetInternalHardwareExtension
    {
        public static UsbHardwareIdAndDriver Internal(this IUsbHidHardware hardware)
        {
            if (hardware is not UsbHardwareIdAndDriver result)
            {
                throw new InvalidOperationException("Failed to cast to internal hardware.");
            }

            return result;
        }
    }
}
