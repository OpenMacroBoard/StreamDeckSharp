namespace StreamDeckSharp
{
    /// <summary>
    /// A collection of Stream Deck USB related constants.
    /// </summary>
    public static class UsbConstants
    {
        /// <summary>
        /// Helper function to create a <see cref="UsbVendorProductPair"/> for Elgato devices
        /// (with the vendor id <see cref="VendorIds.ElgatoSystemsGmbH"/>).
        /// </summary>
        /// <param name="productId">USB product id.</param>
        public static UsbVendorProductPair ElgatoUsbId(int productId)
        {
            return new UsbVendorProductPair(VendorIds.ElgatoSystemsGmbH, productId);
        }

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
    }
}
