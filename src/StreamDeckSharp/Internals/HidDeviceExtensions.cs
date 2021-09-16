using HidSharp;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal static class HidDeviceExtensions
    {
        public static IHardwareInternalInfos GetHardwareInformation(this HidDevice hid)
            => GetDeviceDetails(hid.VendorID, hid.ProductID);

        public static IHardwareInternalInfos GetDeviceDetails(int vendorId, int productId)
        {
            if (vendorId != VendorIds.ElgatoSystemsGmbH)
            {
                return null;
            }

            switch (productId)
            {
                case ProductIds.StreamDeck: return Hardware.Internal_StreamDeck;
                case ProductIds.StreamDeckRev2: return Hardware.Internal_StreamDeckRev2;
                case ProductIds.StreamDeckMK2: return Hardware.Internal_StreamDeckMK2;
                case ProductIds.StreamDeckXL: return Hardware.Internal_StreamDeckXL;
                case ProductIds.StreamDeckMini: return Hardware.Internal_StreamDeckMini;
            }

            return null;
        }
    }
}
