using StreamDeckSharp.Internals;

namespace StreamDeckSharp
{
    /// <summary>
    /// Details about different StreamDeck Hardware
    /// </summary>
    public static class Hardware
    {
        /// <summary>
        /// Details about the classic Stream Deck
        /// </summary>
        public static IUsbHidHardware StreamDeck { get; }

        /// <summary>
        /// Details about the Stream Deck XL
        /// </summary>
        public static IUsbHidHardware StreamDeckXL { get; }

        /// <summary>
        /// Details about the Stream Deck Mini
        /// </summary>
        public static IUsbHidHardware StreamDeckMini { get; }

        internal static IHardwareInternalInfos Internal_StreamDeck { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckXL { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckMini { get; }

        static Hardware()
        {
            var hwStreamDeck = new StreamDeckHardwareInfo();
            var hwStreamDeckXL = new StreamDeckXlHardwareInfo();
            var hwStreamDeckMini = new StreamDeckMiniHardwareInfo();

            StreamDeck = hwStreamDeck;
            StreamDeckXL = hwStreamDeckXL;
            StreamDeckMini = hwStreamDeckMini;

            Internal_StreamDeck = hwStreamDeck;
            Internal_StreamDeckXL = hwStreamDeckXL;
            Internal_StreamDeckMini = hwStreamDeckMini;
        }

        internal static class VendorIds
        {
            public const int ElgatoSystemsGmbH = 0x0fd9;
        }

        internal static class ProductIds
        {
            public const int StreamDeck = 0x0060;
            public const int StreamDeckXL = 0x006c;
            public const int StreamDeckMini = 0x0063;
        }

        internal static IHardwareInternalInfos GetDeviceDetails(int vendorId, int productId)
        {
            if (vendorId != VendorIds.ElgatoSystemsGmbH)
                return null;

            switch (productId)
            {
                case ProductIds.StreamDeck: return Internal_StreamDeck;
                case ProductIds.StreamDeckXL: return Internal_StreamDeckXL;
                case ProductIds.StreamDeckMini: return Internal_StreamDeckMini;
            }

            return null;
        }
    }
}
