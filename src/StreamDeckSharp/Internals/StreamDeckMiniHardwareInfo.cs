using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckMiniHardwareInfo
        : IHardwareInternalInfos
    {
        private const int imgWidth = 80;
        private const int colorChannels = 3;
        private const int pxBorder = (imgWidth - imgWidth) / 2;

        public int KeyCount => keyPositions.Count;
        public int IconSize => imgWidth;
        public int HeaderSize => 16;
        public int ReportSize => 1024;
        public int KeyReportOffset => 0;
        public int UsbVendorId => Hardware.VendorIds.ElgatoSystemsGmbH;
        public int UsbProductId => Hardware.ProductIds.StreamDeckMini;

        public GridKeyPositionCollection Keys
            => keyPositions;

        static StreamDeckMiniHardwareInfo()
        {
            //3x2 keys with 80x80px icons and 25px in between
            keyPositions = new GridKeyPositionCollection(3, 2, imgWidth, 25);
        }

        private static readonly GridKeyPositionCollection keyPositions;
        private static readonly byte[] bmpHeader = new byte[] {
            0x42, 0x4d, 0x36, 0x4b, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x50, 0x00, 0x00, 0x00, 0x50, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x4b, 0x00, 0x00, 0xc4, 0x0e,
            0x00, 0x00, 0xc4, 0x0e, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(imgWidth, imgWidth);
            var bmp = new byte[imgWidth * imgWidth * colorChannels + bmpHeader.Length];

            Array.Copy(bmpHeader, 0, bmp, 0, bmpHeader.Length);

            if (rawData != null)
                for (int y = 0; y < imgWidth; y++)
                    for (int x = 0; x < imgWidth; x++)
                    {
                        var src = (y * imgWidth + x) * colorChannels;
                        var tar = ((imgWidth - x - 1) * imgWidth + y) * colorChannels + bmpHeader.Length;

                        bmp[tar + 0] = rawData[src + 0];
                        bmp[tar + 1] = rawData[src + 1];
                        bmp[tar + 2] = rawData[src + 2];
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
            data[2] = (byte)(pageNumber);
            data[4] = (byte)(isLast ? 1 : 0);
            data[5] = (byte)(keyId + 1);
        }
    }
}
