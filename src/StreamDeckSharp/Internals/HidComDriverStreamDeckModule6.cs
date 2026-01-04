using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    /// <summary>
    /// HID communication driver for Stream Deck Module 6 Keys
    /// Based on official Elgato HID documentation: https://docs.elgato.com/streamdeck/hid/module-6
    /// </summary>
    public sealed class HidComDriverStreamDeckModule6 : IStreamDeckHidComDriver
    {
        private const int ColorChannels = 3;
        private const int ImageSize = 80; // Module 6 uses 80×80 pixel keys per official specs
        private readonly byte[] bmpHeader;

        public HidComDriverStreamDeckModule6()
        {
            this.bmpHeader = GenerateBmpHeader(ImageSize);
        }

        /// <summary>
        /// Generates a BMP header for 8080 pixel images (Module 6 specification)
        /// </summary>
        private static byte[] GenerateBmpHeader(int size)
        {
            int pixelDataSize = size * size * ColorChannels;
            int fileSize = 54 + pixelDataSize;
            
            return new byte[]
            {
                // BMP Header (14 bytes)
                0x42, 0x4d, // 'BM' signature
                (byte)(fileSize & 0xFF), (byte)((fileSize >> 8) & 0xFF), 
                (byte)((fileSize >> 16) & 0xFF), (byte)((fileSize >> 24) & 0xFF),
                0x00, 0x00, 0x00, 0x00, // Reserved
                0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                
                // DIB Header (40 bytes - BITMAPINFOHEADER)
                0x28, 0x00, 0x00, 0x00, // Header size
                (byte)(size & 0xFF), (byte)((size >> 8) & 0xFF), 0x00, 0x00, // Width
                (byte)(size & 0xFF), (byte)((size >> 8) & 0xFF), 0x00, 0x00, // Height
                0x01, 0x00, // Color planes
                0x18, 0x00, // Bits per pixel (24-bit RGB)
                0x00, 0x00, 0x00, 0x00, // No compression
                (byte)(pixelDataSize & 0xFF), (byte)((pixelDataSize >> 8) & 0xFF), 
                (byte)((pixelDataSize >> 16) & 0xFF), 0x00, // Image size
                0xc4, 0x0e, 0x00, 0x00, // X pixels per meter
                0xc4, 0x0e, 0x00, 0x00, // Y pixels per meter
                0x00, 0x00, 0x00, 0x00, // Colors in palette
                0x00, 0x00, 0x00, 0x00, // Important colors
            };
        }

        // Module 6 specifications per official docs
        public int HeaderSize => 16;
        public int ReportSize => 1024; // Module 6 uses 1024-byte output reports
        public int ExpectedFeatureReportLength => 32; // Feature reports are 32 bytes
        public int ExpectedOutputReportLength => 1024;
        public int ExpectedInputReportLength => 65; // Input reports are 65 bytes
        public int KeyReportOffset => 1;
        public byte FirmwareVersionFeatureId => 0xA1; // AP2 (Primary firmware)
        public byte SerialNumberFeatureId => 0x03;
        public int FirmwareVersionReportSkip => 5;
        public int SerialNumberReportSkip => 5;
        public double BytesPerSecondLimit => double.PositiveInfinity;

        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(ImageSize, ImageSize);
            var bmp = new byte[ImageSize * ImageSize * ColorChannels + bmpHeader.Length];
            Array.Copy(bmpHeader, 0, bmp, 0, bmpHeader.Length);

            if (rawData.Length != 0)
            {
                // Rotate image 90 clockwise as per Module 6 specs
                for (var y = 0; y < ImageSize; y++)
                {
                    for (var x = 0; x < ImageSize; x++)
                    {
                        var src = (y * ImageSize + x) * ColorChannels;
                        var tar = ((ImageSize - x - 1) * ImageSize + y) * ColorChannels + bmpHeader.Length;
                        bmp[tar + 0] = rawData[src + 0];
                        bmp[tar + 1] = rawData[src + 1];
                        bmp[tar + 2] = rawData[src + 2];
                    }
                }
            }
            return bmp;
        }

        public int ExtKeyIdToHardwareKeyId(int extKeyId) => extKeyId;
        public int HardwareKeyIdToExtKeyId(int hardwareKeyId) => hardwareKeyId;

        /// <summary>
        /// Prepares packet header for Module 6 image upload
        /// Per official docs: https://docs.elgato.com/streamdeck/hid/module-6/#upload-data-to-image-memory-bank
        /// Offset 0x00: Report ID (0x02)
        /// Offset 0x01: Command (0x01)
        /// Offset 0x02: Chunk Index
        /// Offset 0x03: Reserved (0x00)
        /// Offset 0x04: Show Image flag (0x01 to display immediately)
        /// Offset 0x05: Key Index
        /// Offset 0x06-0x0F: Reserved (10 bytes of 0x00)
        /// Offset 0x10+: Chunk Data
        /// </summary>
        public void PrepareDataForTransmission(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast)
        {
            data[0] = 0x02; // Report ID
            data[1] = 0x01; // Command: Upload Data to Image Memory Bank
            data[2] = (byte)pageNumber; // Chunk Index
            data[3] = 0x00; // Reserved
            data[4] = (byte)(isLast ? 0x01 : 0x00); // Show Image flag (show on last packet)
            data[5] = (byte)(keyId + 1); // Key Index (0-5 for Module 6)
            
            // Reserved space (bytes 6-15, total 10 bytes)
            for (int i = 6; i < 16; i++)
            {
                data[i] = 0x00;
            }
            
            // Payload data starts at offset 0x10 (byte 16)
        }

        /// <summary>
        /// Creates brightness control message for Module 6
        /// Per official docs Report ID: 0x05, Command: 0x55
        /// </summary>
        public byte[] GetBrightnessMessage(byte percent)
        {
            if (percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            
            // Feature Report format for Set Backlight Brightness
            var buffer = new byte[32]; // Feature reports are 32 bytes, zero-padded
            buffer[0] = 0x05; // Report ID
            buffer[1] = 0x55; // Command
            buffer[2] = 0xAA;
            buffer[3] = 0xD1;
            buffer[4] = 0x01;
            buffer[5] = percent; // Brightness value (0-100)
            
            return buffer;
        }

        /// <summary>
        /// Creates show logo message for Module 6
        /// Per official docs Report ID: 0x0B, Command: 0x63, Payload: 0x00
        /// </summary>
        public byte[] GetLogoMessage()
        {
            var buffer = new byte[32]; // Feature reports are 32 bytes, zero-padded
            buffer[0] = 0x0B; // Report ID
            buffer[1] = 0x63; // Command
            buffer[2] = 0x00; // Show Boot Logo
            
            return buffer;
        }
    }
}
