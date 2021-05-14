using OpenMacroBoard.SDK;
using System;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckHardwareInfo
        : IHardwareInternalInfos
    {
        private const int ImgWidth = 72;
        private const int ColorChannels = 3;

        private static readonly GridKeyPositionCollection KeyPositions
            = new GridKeyPositionCollection(5, 3, ImgWidth, 30);

        private static readonly byte[] BmpHeader = new byte[]
        {
            0x42, 0x4d, 0xf6, 0x3c, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xc0, 0x3c, 0x00, 0x00, 0xc4, 0x0e,
            0x00, 0x00, 0xc4, 0x0e, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public int KeyCount => KeyPositions.Count;
        public int IconSize => ImgWidth;
        public int HeaderSize => 16;
        public int ReportSize => 7819;
        public int KeyReportOffset => 1;
        public int UsbVendorId => VendorIds.ElgatoSystemsGmbH;
        public int UsbProductId => ProductIds.StreamDeck;
        public string DeviceName => "Stream Deck";
        public byte FirmwareVersionFeatureId => 4;
        public byte SerialNumberFeatureId => 3;
        public int FirmwareReportSkip => 5;
        public int SerialNumberReportSkip => 5;
        public GridKeyPositionCollection Keys
            => KeyPositions;

        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(ImgWidth, ImgWidth);

            var bmp = new byte[ImgWidth * ImgWidth * 3 + BmpHeader.Length];
            Array.Copy(BmpHeader, 0, bmp, 0, BmpHeader.Length);

            if (rawData != null)
            {
                for (var y = 0; y < ImgWidth; y++)
                {
                    for (var x = 0; x < ImgWidth; x++)
                    {
                        var src = (y * ImgWidth + x) * ColorChannels;
                        var tar = (y * ImgWidth + ((ImgWidth - 1) - x)) * ColorChannels + BmpHeader.Length;

                        bmp[tar + 0] = rawData[src + 0];
                        bmp[tar + 1] = rawData[src + 1];
                        bmp[tar + 2] = rawData[src + 2];
                    }
                }
            }

            return bmp;
        }

        public int ExtKeyIdToHardwareKeyId(int extKeyId)
            => FlipIdsHorizontal(extKeyId);

        public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
            => FlipIdsHorizontal(hardwareKeyId);

        public void PrepareDataForTransmittion(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast)
        {
            data[0] = 2; // Report ID ?
            data[1] = 1; // ?
            data[2] = (byte)(pageNumber + 1);
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

        private static int FlipIdsHorizontal(int keyId)
        {
            var diff = ((keyId % 5) - 2) * -2;
            return keyId + diff;
        }
    }
}
