using OpenMacroBoard.SDK;

namespace StreamDeckSharp.Internals
{
    internal interface IHardwareInternalInfos : IUsbHidHardware
    {
        int HeaderSize { get; }
        int ReportSize { get; }
        int KeyReportOffset { get; }
        byte FirmwareVersionFeatureId { get; }
        int FirmwareReportSkip { get; }
        byte SerialNumberFeatureId { get; }
        int SerialNumberReportSkip { get; }

        byte[] GeneratePayload(KeyBitmap keyBitmap);

        /// <summary>
        /// This is used to convert between keyId conventions
        /// </summary>
        /// <param name="extKeyId"></param>
        /// <returns></returns>
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
        /// <returns></returns>
        int HardwareKeyIdToExtKeyId(int hardwareKeyId);

        void PrepareDataForTransmittion(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast);

        byte[] GetBrightnessMessage(byte percent);
        byte[] GetLogoMessage();
    }
}
