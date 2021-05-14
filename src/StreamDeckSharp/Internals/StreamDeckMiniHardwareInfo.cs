using OpenMacroBoard.SDK;
using System;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckMiniHardwareInfo
        : IHardwareInternalInfos
    {
        private const int ImgWidth = 80;
        private const int ColorChannels = 3;

        private static readonly GridKeyPositionCollection KeyPositions
             = new GridKeyPositionCollection(3, 2, ImgWidth, 32);

        private static readonly byte[] BmpHeader = new byte[]
        {
            0x42, 0x4d, 0x36, 0x4b, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x50, 0x00, 0x00, 0x00, 0x50, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x4b, 0x00, 0x00, 0xc4, 0x0e,
            0x00, 0x00, 0xc4, 0x0e, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public int KeyCount => KeyPositions.Count;
        public int IconSize => ImgWidth;
        public int HeaderSize => 16;
        public int ReportSize => 1024;
        public int KeyReportOffset => 1;
        public int UsbVendorId => VendorIds.ElgatoSystemsGmbH;
        public int UsbProductId => ProductIds.StreamDeckMini;
        public string DeviceName => "Stream Deck Mini";
        public byte FirmwareVersionFeatureId => 4;
        public byte SerialNumberFeatureId => 3;
        public int FirmwareReportSkip => 5;
        public int SerialNumberReportSkip => 5;

        public GridKeyPositionCollection Keys
            => KeyPositions;

        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(ImgWidth, ImgWidth);
            var bmp = new byte[ImgWidth * ImgWidth * ColorChannels + BmpHeader.Length];

            Array.Copy(BmpHeader, 0, bmp, 0, BmpHeader.Length);

            if (rawData != null)
            {
                for (var y = 0; y < ImgWidth; y++)
                {
                    for (var x = 0; x < ImgWidth; x++)
                    {
                        var src = (y * ImgWidth + x) * ColorChannels;
                        var tar = ((ImgWidth - x - 1) * ImgWidth + y) * ColorChannels + BmpHeader.Length;

                        bmp[tar + 0] = rawData[src + 0];
                        bmp[tar + 1] = rawData[src + 1];
                        bmp[tar + 2] = rawData[src + 2];
                    }
                }
            }

            return bmp;
        }

        public int ExtKeyIdToHardwareKeyId(int extKeyId)
            => extKeyId;

        public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
            => hardwareKeyId;

        public void PrepareDataForTransmittion(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast)
        {
            data[0] = 2; // Report ID ?
            data[1] = 1; // ?
            data[2] = (byte)pageNumber;
            data[4] = (byte)(isLast ? 1 : 0);
            data[5] = (byte)(keyId + 1);
        }

        public byte[] GetBrightnessMessage(byte percent)
        {
            if (percent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent));
            }

            var buffer = new byte[] { 0x05, 0x55, 0xaa, 0xd1, 0x01, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            buffer[5] = percent;
            return buffer;
        }

        public byte[] GetLogoMessage()
        {
            return new byte[] { 0x0B, 0x63 };
        }
    }
}
