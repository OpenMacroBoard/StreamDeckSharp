using System;
using HidLibrary;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal static class HidDeviceExtensions
    {
        public static IHardwareInternalInfos GetHardwareInformation(this HidDevice hid)
            => GetDeviceDetails(hid.Attributes.VendorId, hid.Attributes.ProductId);

        public static IHardwareInternalInfos GetDeviceDetails(int vendorId, int productId)
        {
            if (vendorId != VendorIds.ElgatoSystemsGmbH)
                return null;

            switch (productId)
            {
                case ProductIds.StreamDeck: return Hardware.Internal_StreamDeck;
                case ProductIds.StreamDeckXL: return Hardware.Internal_StreamDeckXL;
                case ProductIds.StreamDeckMini: return Hardware.Internal_StreamDeckMini;
            }

            return null;
        }
    }
}
