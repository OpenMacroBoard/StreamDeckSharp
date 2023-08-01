using HidSharp;

namespace StreamDeckSharp.Internals
{
    internal static class HidDeviceExtensions
    {
        public static UsbHardwareIdAndDriver GetHardwareInformation(this HidDevice hid)
        {
            return Hardware.GetInternalHardwareInfos(new(hid.VendorID, hid.ProductID));
        }
    }
}
