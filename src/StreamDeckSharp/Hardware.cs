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
    }
}
