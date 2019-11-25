using StreamDeckSharp.Internals;
using static StreamDeckSharp.UsbConstants;

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
        /// Details about the classic Stream Deck Rev 2
        /// </summary>
        public static IUsbHidHardware StreamDeckRev2 { get; }

        /// <summary>
        /// Details about the Stream Deck XL
        /// </summary>
        public static IUsbHidHardware StreamDeckXL { get; }

        /// <summary>
        /// Details about the Stream Deck Mini
        /// </summary>
        public static IUsbHidHardware StreamDeckMini { get; }

        internal static IHardwareInternalInfos Internal_StreamDeck { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckRev2 { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckXL { get; }
        internal static IHardwareInternalInfos Internal_StreamDeckMini { get; }

        static Hardware()
        {
            var hwStreamDeck = new StreamDeckHardwareInfo();
            var hwStreamDeckRev2 = new StreamDeckRev2HardwareInfo();
            var hwStreamDeckXL = new StreamDeckXlHardwareInfo();
            var hwStreamDeckMini = new StreamDeckMiniHardwareInfo();

            StreamDeck = hwStreamDeck;
            StreamDeckRev2 = hwStreamDeckRev2;
            StreamDeckXL = hwStreamDeckXL;
            StreamDeckMini = hwStreamDeckMini;

            Internal_StreamDeck = hwStreamDeck;
            Internal_StreamDeckRev2 = hwStreamDeckRev2;
            Internal_StreamDeckXL = hwStreamDeckXL;
            Internal_StreamDeckMini = hwStreamDeckMini;
        }
    }
}
