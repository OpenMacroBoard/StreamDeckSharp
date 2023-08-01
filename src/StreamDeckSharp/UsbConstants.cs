namespace StreamDeckSharp
{
    /// <summary>
    /// A collection of Stream Deck USB related constants.
    /// </summary>
    public static class UsbConstants
    {
        /// <summary>
        /// Known (Stream Deck related) USB Vendor IDs.
        /// </summary>
        public static class VendorIds
        {
            /// <summary>
            /// The USB Vendor ID for Elgato Systems GmbH.
            /// </summary>
            public const int ElgatoSystemsGmbH = 0x0fd9;
        }

        /// <summary>
        /// Known (Stream Deck related) USB Product IDs.
        /// </summary>
        public static class ProductIds
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            public const int StreamDeck = 0x0060;
            public const int StreamDeckRev2 = 0x006d;
            public const int StreamDeckMK2 = 0x0080;
            public const int StreamDeckXL = 0x006c;
            public const int StreamDeckXLRev2 = 0x008f;
            public const int StreamDeckMini = 0x0063;
            public const int StreamDeckMiniRev2 = 0x0090;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
