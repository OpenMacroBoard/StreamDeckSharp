using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    /// <summary>
    /// HID Stream Deck communication driver for the classical Stream Deck.
    /// </summary>
    public sealed class HidComDriverStreamDeck
        : IStreamDeckHidComDriver
    {
        private const int ImgWidth = 72;
        private const int ColorChannels = 3;

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

        /// <inheritdoc/>
        public int HeaderSize => 16;

        /// <inheritdoc/>
        public int ReportSize => 7819;

        /// <inheritdoc/>
        public int ExpectedFeatureReportLength => 17;

        /// <inheritdoc/>
        public int ExpectedOutputReportLength => 8191;

        /// <inheritdoc/>
        public int ExpectedInputReportLength => 17;

        /// <inheritdoc/>
        public int KeyReportOffset => 1;

        /// <inheritdoc/>
        public byte FirmwareVersionFeatureId => 4;

        /// <inheritdoc/>
        public byte SerialNumberFeatureId => 3;

        /// <inheritdoc/>
        public int FirmwareVersionReportSkip => 5;

        /// <inheritdoc/>
        public int SerialNumberReportSkip => 5;

        /// <inheritdoc/>
        public double BytesPerSecondLimit { get; set; } = double.PositiveInfinity;

        /// <inheritdoc/>
        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(ImgWidth, ImgWidth);

            var bmp = new byte[ImgWidth * ImgWidth * 3 + BmpHeader.Length];
            Array.Copy(BmpHeader, 0, bmp, 0, BmpHeader.Length);

            if (rawData.Length != 0)
            {
                for (var y = 0; y < ImgWidth; y++)
                {
                    for (var x = 0; x < ImgWidth; x++)
                    {
                        var src = (y * ImgWidth + x) * ColorChannels;
                        var tar = (y * ImgWidth + (ImgWidth - 1 - x)) * ColorChannels + BmpHeader.Length;

                        bmp[tar + 0] = rawData[src + 0];
                        bmp[tar + 1] = rawData[src + 1];
                        bmp[tar + 2] = rawData[src + 2];
                    }
                }
            }

            return bmp;
        }

        /// <inheritdoc/>
        public int ExtKeyIdToHardwareKeyId(int extKeyId)
        {
            return FlipIdsHorizontal(extKeyId);
        }

        /// <inheritdoc/>
        public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
        {
            return FlipIdsHorizontal(hardwareKeyId);
        }

        /// <inheritdoc/>
        public void PrepareDataForTransmission(
            byte[] data,
            int pageNumber,
            int payloadLength,
            int keyId,
            bool isLast
        )
        {
            data[0] = 2; // Report ID ?
            data[1] = 1; // ?
            data[2] = (byte)(pageNumber + 1);
            data[4] = (byte)(isLast ? 1 : 0);
            data[5] = (byte)(keyId + 1);
        }

        /// <inheritdoc/>
        public byte[] GetBrightnessMessage(byte percent)
        {
            if (percent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent));
            }

            var buffer = new byte[]
            {
                0x05, 0x55, 0xaa, 0xd1, 0x01, 0x64, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00,
            };

            buffer[5] = percent;
            return buffer;
        }

        /// <inheritdoc/>
        public byte[] GetLogoMessage()
        {
            return new byte[] { 0x0B, 0x63 };
        }

        /// <inheritdoc/>
        private static int FlipIdsHorizontal(int keyId)
        {
            var diff = ((keyId % 5) - 2) * -2;
            return keyId + diff;
        }
    }
}
