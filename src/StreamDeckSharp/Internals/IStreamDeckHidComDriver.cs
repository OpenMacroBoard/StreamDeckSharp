using OpenMacroBoard.SDK;

namespace StreamDeckSharp.Internals
{
    /// <summary>
    /// Interface that describes the StreamDeck HID communication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unless there are new stream deck versions you likely don't have to deal with this interface
    /// or implementations of it directly as a consumer of the library. Because this interface is
    /// low level, some members may not have a good documentation or any at all.
    /// </para>
    /// <para>Implementations must be thread-safe.</para>
    /// </remarks>
    public interface IStreamDeckHidComDriver
    {
        /// <summary>
        /// Gets the header size.
        /// </summary>
        int HeaderSize { get; }

        /// <summary>
        /// Gets the report size.
        /// </summary>
        int ReportSize { get; }

        /// <summary>
        /// Gets the feature report length for the device.
        /// </summary>
        /// <remarks>
        /// This is asserted (in debug mode).
        /// </remarks>
        int ExpectedFeatureReportLength { get; }

        /// <summary>
        /// Gets the output report length for the device.
        /// </summary>
        /// <remarks>
        /// This is asserted (in debug mode).
        /// </remarks>
        int ExpectedOutputReportLength { get; }

        /// <summary>
        /// Gets the input wreport length for the device.
        /// </summary>
        /// <remarks>
        /// This is asserted (in debug mode).
        /// </remarks>
        int ExpectedInputReportLength { get; }

        /// <summary>
        /// Gets the offset of the key information inside the key state report.
        /// </summary>
        int KeyReportOffset { get; }

        /// <summary>
        /// The ID of the feature that identifies the firmware version.
        /// </summary>
        byte FirmwareVersionFeatureId { get; }

        /// <summary>
        /// Number of bytes to skip before the firmware version string starts.
        /// </summary>
        /// <remarks>
        /// For details see property documentation of <see cref="SerialNumberReportSkip"/>.
        /// </remarks>
        int FirmwareVersionReportSkip { get; }

        /// <summary>
        /// The ID of the feature that identifies the serial number.
        /// </summary>
        byte SerialNumberFeatureId { get; }

        /// <summary>
        /// Number of bytes to skip before the serial number string starts.
        /// </summary>
        /// <remarks>
        /// For some reason some string reports have some "weird" data prefixed.
        /// I guess they are some binary encoded details or headers - no idea.
        /// This property can be tweaked so the resulting string doesn't contain
        /// strange unicode characters.
        /// </remarks>
        int SerialNumberReportSkip { get; }

        /// <summary>
        /// Limits the USB transfer speed.
        /// </summary>
        /// <remarks>
        /// Some stream decks produce artifacts and glitches when data comes in to fast.
        /// I'm not sure if this happens because of this library or because of a bug in
        /// the stream deck's firmware but currently the work-around is to limit the transfer rate.
        /// This value has to be determined experimentally.
        /// </remarks>
        double BytesPerSecondLimit { get; }

        /// <summary>
        /// Generate they payload for a given <paramref name="keyBitmap"/>.
        /// </summary>
        byte[] GeneratePayload(KeyBitmap keyBitmap);

        /// <summary>
        /// This is used to convert between keyId conventions
        /// </summary>
        /// <remarks>
        /// The original stream deck has a pretty weird way of enumerating keys.
        /// Index 0 starts right top and they are enumerated right to left,
        /// and top to bottom. Most developers would expect it to be left-to-right
        /// instead of right-to-left, so we change that ;-)
        /// </remarks>
        int ExtKeyIdToHardwareKeyId(int extKeyId);

        /// <summary>
        /// This is used to convert between keyId conventions
        /// </summary>
        /// <param name="hardwareKeyId"></param>
        int HardwareKeyIdToExtKeyId(int hardwareKeyId);

        /// <summary>
        /// Before the report is sent to the stream deck (human interface device) this is called to
        /// prepare meta information and details in the report header. This depends on the target device
        /// and has to be reverse engineered with a USB traffic analyzer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pageNumber"></param>
        /// <param name="payloadLength"></param>
        /// <param name="keyId"></param>
        /// <param name="isLast"></param>
        void PrepareDataForTransmission(
            byte[] data,
            int pageNumber,
            int payloadLength,
            int keyId,
            bool isLast
        );

        /// <summary>
        /// Generates a message to set a given brightness.
        /// </summary>
        byte[] GetBrightnessMessage(byte percent);

        /// <summary>
        /// Generates a message to show the vendor logo.
        /// </summary>
        byte[] GetLogoMessage();
    }
}
