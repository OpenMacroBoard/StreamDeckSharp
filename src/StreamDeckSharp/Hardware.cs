using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using static StreamDeckSharp.UsbConstants;

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
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeck
                    ),
                    "Stream Deck",
                    streamDeckKeys,
                    new HidComDriverStreamDeck()
                    {
                        // Limit of 3'200'000 bytes/s (~3.0 MiB/s)
                        // because without that limit glitches will happen on fast writes.
                        BytesPerSecondLimit = 3_200_000,
                    }
                );

            var streamDeckKeysNew = new GridKeyLayout(5, 3, 72, 32);

            StreamDeckMK2 =
                RegisterNewHardwareInternal(
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeckMK2
                    ),
                    "Stream Deck MK.2",
                    streamDeckKeysNew,
                    new HidComDriverStreamDeckJpeg(72)
                    {
                        // Limit of 1'500'000 bytes/s (~1.5 MB/s),
                        // because ImageGlitchTest.Rainbow has glitches with higher speeds
                        BytesPerSecondLimit = 1_500_000,
                    }
                );

            StreamDeckRev2 =
                RegisterNewHardwareInternal(
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeckRev2
                    ),
                    "Stream Deck Rev2",
                    streamDeckKeysNew,
                    new HidComDriverStreamDeckJpeg(72)
                    {
                        // Limit of 3'200'000 bytes/s (~3.0 MiB/s) just to be safe,
                        // because I don't own a StreamDeck Rev2 to test it.
                        BytesPerSecondLimit = 3_200_000,
                    }
                );

            // .----------------.
            // | Stream Deck XL |
            // '----------------'

            var streamDeckXlkeys = new GridKeyLayout(8, 4, 96, 38);
            var streamDeckXlDriver = new HidComDriverStreamDeckJpeg(96);

            StreamDeckXL =
                RegisterNewHardwareInternal(
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeckXL
                    ),
                    "Stream Deck XL",
                    streamDeckXlkeys,
                    streamDeckXlDriver
                );

            StreamDeckXlRev2 =
                RegisterNewHardwareInternal(
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeckXLRev2
                    ),
                    "Stream Deck XL Rev2",
                    streamDeckXlkeys,
                    streamDeckXlDriver
                );

            // .------------------.
            // | Stream Deck Mini |
            // '------------------'

            var streamDeckMiniKeys = new GridKeyLayout(3, 2, 80, 32);
            var streamDeckMiniDriver = new HidComDriverStreamDeckMini(80);

            StreamDeckMini =
                RegisterNewHardwareInternal(
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeckMini
                    ),
                    "Stream Deck Mini",
                    streamDeckMiniKeys,
                    streamDeckMiniDriver
                );

            SteamDeckMiniRev2 =
                RegisterNewHardwareInternal(
                    new
                    (
                        VendorIds.ElgatoSystemsGmbH,
                        ProductIds.StreamDeckMiniRev2
                    ),
                    "Stream Deck Mini Rev2",
                    streamDeckMiniKeys,
                    streamDeckMiniDriver
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
        /// Details about the Stream Deck XL Rev2
        /// </summary>
        public static IUsbHidHardware StreamDeckXlRev2 { get; }

        /// <summary>
        /// Details about the Stream Deck Mini
        /// </summary>
        public static IUsbHidHardware StreamDeckMini { get; }

        /// <summary>
        /// Details about the Stream Deck Mini Rev2
        /// </summary>
        public static IUsbHidHardware SteamDeckMiniRev2 { get; }

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
            return RegisterNewHardwareInternal(usbId, deviceName, keyLayout, driver);
        }

        internal static UsbHardwareIdAndDriver RegisterNewHardwareInternal(
            UsbVendorProductPair usbId,
            string deviceName,
            GridKeyLayout keyLayout,
            IStreamDeckHidComDriver driver
        )
        {
            var internalReference = new UsbHardwareIdAndDriver(usbId, deviceName, keyLayout, driver);

            RegisteredHardware.AddOrUpdate(usbId, internalReference, (_, _) => internalReference);

            return internalReference;
        }

        internal static IEnumerable<UsbHardwareIdAndDriver> GetInternalStreamDeckHardwareInfos()
        {
            return RegisteredHardware.Values.ToList();
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
