using HidSharp;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal static class HidDeviceExtensions
    {
        public static IHardwareInternalInfos GetHardwareInformation(this HidDevice hid)
        {
            return GetDeviceDetails(hid.VendorID, hid.ProductID);
        }

        public static IHardwareInternalInfos GetDeviceDetails(int vendorId, int productId)
        {
            if (vendorId != VendorIds.ElgatoSystemsGmbH)
            {
                return null;
            }

            return productId switch
            {
                ProductIds.StreamDeck => Hardware.Internal_StreamDeck,
                ProductIds.StreamDeckRev2 => Hardware.Internal_StreamDeckRev2,
                ProductIds.StreamDeckMK2 => Hardware.Internal_StreamDeckMK2,
                ProductIds.StreamDeckXL => Hardware.Internal_StreamDeckXL,
                ProductIds.StreamDeckMini => Hardware.Internal_StreamDeckMini,
                _ => null,
            };
        }
    }
}
