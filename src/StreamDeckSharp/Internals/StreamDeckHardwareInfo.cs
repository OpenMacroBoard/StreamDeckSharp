using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckHardwareInfo
        : IHardwareInternalInfos
    {
        private const int ImgWidth = 72;
        private const int ColorChannels = 3;

        public int KeyCount => keyPositions.Count;
        public int IconSize => ImgWidth;
        public int ReportSize => 7803;
        public int StartReportNumber => 1;
        public int UsbVendorId => Hardware.VendorIds.ElgatoSystemsGmbH;
        public int UsbProductId => Hardware.ProductIds.StreamDeck;

        public IKeyPositionCollection Keys
           => keyPositions;

        static StreamDeckHardwareInfo()
        {
            //3x2 keys with 72x72px icons and 25px in between
            keyPositions = new KeyPositionCollection(5, 3, ImgWidth, 25);
        }

        private static readonly KeyPositionCollection keyPositions;
        private static readonly byte[] bmpHeader = new byte[] {
            0x42, 0x4d, 0xf6, 0x3c, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xc0, 0x3c, 0x00, 0x00, 0xc4, 0x0e,
            0x00, 0x00, 0xc4, 0x0e, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(ImgWidth, ImgWidth);

            var bmp = new byte[ImgWidth * ImgWidth * 3 + bmpHeader.Length];
            Array.Copy(bmpHeader, 0, bmp, 0, bmpHeader.Length);

            if (rawData != null)
                for (int y = 0; y < ImgWidth; y++)
                    for (int x = 0; x < ImgWidth; x++)
                    {
                        var src = (y * ImgWidth + x) * ColorChannels;
                        var tar = (y * ImgWidth + ((ImgWidth - 1) - x)) * ColorChannels + bmpHeader.Length;

                        bmp[tar + 0] = rawData[src + 0];
                        bmp[tar + 1] = rawData[src + 1];
                        bmp[tar + 2] = rawData[src + 2];
                    }

            return bmp;
        }

        public int ExtKeyIdToHardwareKeyId(int extKeyId)
            => FlipIdsHorizontal(extKeyId);

        public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
            => FlipIdsHorizontal(hardwareKeyId);

        private static int FlipIdsHorizontal(int keyId)
        {
            var diff = ((keyId % 5) - 2) * -2;
            return keyId + diff;
        }
    }
}
