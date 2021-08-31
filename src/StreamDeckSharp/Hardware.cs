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
        public static IUsbHidHardware StreamDeck
            => Internal_StreamDeck;

        /// <summary>
        /// Details about the updated Stream Deck MK.2
        /// </summary>
        public static IUsbHidHardware StreamDeckMK2
            => Internal_StreamDeckMK2;

        /// <summary>
        /// Details about the classic Stream Deck Rev 2
        /// </summary>
        public static IUsbHidHardware StreamDeckRev2
            => Internal_StreamDeckRev2;

        /// <summary>
        /// Details about the Stream Deck XL
        /// </summary>
        public static IUsbHidHardware StreamDeckXL
            => Internal_StreamDeckXL;

        /// <summary>
        /// Details about the Stream Deck Mini
        /// </summary>
        public static IUsbHidHardware StreamDeckMini
            => Internal_StreamDeckMini;

        internal static IHardwareInternalInfos Internal_StreamDeck { get; }
            = new StreamDeckHardwareInfo();

        internal static IHardwareInternalInfos Internal_StreamDeckRev2 { get; }
            = new StreamDeckRev2HardwareInfo();

        internal static IHardwareInternalInfos Internal_StreamDeckMK2 { get; }
            = new StreamDeckMK2HardwareInfo();

        internal static IHardwareInternalInfos Internal_StreamDeckXL { get; }
            = new StreamDeckXlHardwareInfo();

        internal static IHardwareInternalInfos Internal_StreamDeckMini { get; }
            = new StreamDeckMiniHardwareInfo();
    }
}
