using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using static StreamDeckSharp.UsbConstants;

#pragma warning disable AV1710 // Member name includes the name of its containing type

namespace StreamDeckSharp
{
    /// <summary>
    /// Details about different StreamDeck Hardware
    /// </summary>
    public static class Hardware
    {
        private static readonly ConcurrentDictionary<UsbVendorProductPair, UsbHardwareIdAndDriver> RegisteredHardware = new();

        static Hardware()
        {
            // .-------------.
            // | Stream Deck |
            // '-------------'

            var streamDeckKeys = new GridKeyLayout(5, 3, 72, 30);

            StreamDeck =
                RegisterNewHardwareInternal(
                    "Stream Deck",
                    streamDeckKeys,
                    new HidComDriverStreamDeck()
                    {
                        // Limit of 3'200'000 bytes/s (~3.0 MiB/s)
                        // because without that limit glitches will happen on fast writes.
                        BytesPerSecondLimit = 3_200_000,
                    },
                    ElgatoUsbId(0x0060)
                );

            var streamDeckKeysNew = new GridKeyLayout(5, 3, 72, 32);

            StreamDeckMK2 =
                RegisterNewHardwareInternal(
                    "Stream Deck MK.2",
                    streamDeckKeysNew,
                    new HidComDriverStreamDeckJpeg(72)
                    {
                        // Limit of 1'500'000 bytes/s (~1.5 MB/s),
                        // because ImageGlitchTest.Rainbow has glitches with higher speeds
                        BytesPerSecondLimit = 1_500_000,
                    },
                    ElgatoUsbId(0x0080)
                );

            StreamDeckRev2 =
                RegisterNewHardwareInternal(
                    "Stream Deck Rev2",
                    streamDeckKeysNew,
                    new HidComDriverStreamDeckJpeg(72)
                    {
                        // Limit of 3'200'000 bytes/s (~3.0 MiB/s) just to be safe,
                        // because I don't own a StreamDeck Rev2 to test it.
                        BytesPerSecondLimit = 3_200_000,
                    },
                    ElgatoUsbId(0x006d)
                );

            // .----------------.
            // | Stream Deck XL |
            // '----------------'

            StreamDeckXL =
                RegisterNewHardwareInternal(
                    "Stream Deck XL",
                    new GridKeyLayout(8, 4, 96, 38),
                    new HidComDriverStreamDeckJpeg(96),
                    ElgatoUsbId(0x006c),
                    ElgatoUsbId(0x008f),
                    ElgatoUsbId(0x00ba)
                );

            // .------------------.
            // | Stream Deck Mini |
            // '------------------'

            StreamDeckMini =
                RegisterNewHardwareInternal(
                    "Stream Deck Mini",
                    new GridKeyLayout(3, 2, 80, 32),
                    new HidComDriverStreamDeckMini(80),
                    ElgatoUsbId(0x0063),
                    ElgatoUsbId(0x0090)
                );
        }

        /// <summary>
        /// Details about the classic Stream Deck
        /// </summary>
        public static IUsbHidHardware StreamDeck { get; }

        /// <summary>
        /// Details about the updated Stream Deck MK.2
        /// </summary>
        public static IUsbHidHardware StreamDeckMK2 { get; }

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

        /// <summary>
        /// This method registers a new (currently unknown to this library) hardware driver.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method can be used if a new stream deck hardware is released to the market and
        /// the library currently doesn't have support for that new device. In the past a new device
        /// was often pretty similar to an existing device so with this method a tech-savvy person
        /// can register that new device.
        /// </para>
        /// <para>
        /// This feature is a bit "low-level", just take a look at the source code
        /// if you are not sure what to do.
        /// </para>
        /// </remarks>
        /// <param name="usbId">The USB vendor and product ID.</param>
        /// <param name="deviceName">A human readable name of the device.</param>
        /// <param name="keyLayout">The key layout of the device.</param>
        /// <param name="driver">The code that is used to communicate to the device.</param>
        /// <returns>
        /// Returns a description of the device that can be used to open that device with
        /// <see cref="StreamDeck.OpenDevice(IUsbHidHardware[])"/> or
        /// <see cref="StreamDeck.EnumerateDevices(IUsbHidHardware[])"/>.
        /// </returns>
        public static IUsbHidHardware RegisterNewHardware(
            UsbVendorProductPair usbId,
            string deviceName,
            GridKeyLayout keyLayout,
            IStreamDeckHidComDriver driver
        )
        {
            return RegisterNewHardwareInternal(
                deviceName,
                keyLayout,
                driver,
                usbId
            );
        }

        internal static UsbHardwareIdAndDriver RegisterNewHardwareInternal(
            string deviceName,
            GridKeyLayout keyLayout,
            IStreamDeckHidComDriver driver,
            params UsbVendorProductPair[] usbIds
        )
        {
            var internalReference = new UsbHardwareIdAndDriver(
                usbIds.ToList(),
                deviceName,
                keyLayout,
                driver
            );

            foreach (var id in usbIds)
            {
                RegisteredHardware.AddOrUpdate(id, internalReference, (_, _) => internalReference);
            }

            return internalReference;
        }

        internal static IEnumerable<UsbHardwareIdAndDriver> GetInternalStreamDeckHardwareInfos()
        {
            return RegisteredHardware.Values.Distinct().ToList();
        }

        internal static UsbHardwareIdAndDriver GetInternalHardwareInfos(UsbVendorProductPair usbId)
        {
            if (RegisteredHardware.TryGetValue(usbId, out var hardwareInfo))
            {
                return hardwareInfo;
            }

            return null;
        }
    }
}
