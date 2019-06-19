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
        /// Details about the Stream Deck Mini
        /// </summary>
        public static IUsbHidHardware StreamDeckMini { get; }

        /// <summary>
        /// Details about the Stream Deck XL
        /// </summary>
        public static IUsbHidHardware StreamDeckXL { get; }

        internal static IHardwareInternalInfos Internal_StreamDeck { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckMini { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckXL { get; }

        static Hardware()
        {
            var hwStreamDeck = new StreamDeckHardwareInfo();
            var hwStreamDeckMini = new StreamDeckMiniHardwareInfo();
            var hwStreamDeckXL = new StreamDeckXlHardwareInfo();

            StreamDeck = hwStreamDeck;
            StreamDeckMini = hwStreamDeckMini;
            StreamDeckXL = hwStreamDeckXL;

            Internal_StreamDeck = hwStreamDeck;
            Internal_StreamDeckMini = hwStreamDeckMini;
            Internal_StreamDeckXL = hwStreamDeckXL;
        }

        internal static class VendorIds
        {
            public const int ElgatoSystemsGmbH = 0x0fd9;
        }

        internal static class ProductIds
        {
            public const int StreamDeck = 0x0060;
            public const int StreamDeckMini = 0x0063;
            public const int StreamDeckXL = 0x006c;
        }

        internal static bool IsKnownDevice(int vendorId, int productId)
        {
            if (vendorId != VendorIds.ElgatoSystemsGmbH)
                return false;

            switch (productId)
            {
                case ProductIds.StreamDeck:
                case ProductIds.StreamDeckMini:
                case ProductIds.StreamDeckXL:
                    return true;
                default:
                    return false;
            }
        }
    }
}
