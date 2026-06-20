using System;

namespace StreamDeckSharp.Internals
{
    /// <summary>
    /// HID communication driver for Stream Deck Module 6 Keys
    /// Based on official Elgato HID documentation: https://docs.elgato.com/streamdeck/hid/module-6
    /// Inherits shared image processing logic from <see cref="HidComDriverStreamDeckMini"/>.
    /// </summary>
    public sealed class HidComDriverStreamDeckModule6 : HidComDriverStreamDeckMini
    {
        private const int ImageSize = 80; // Module 6 uses 80�80 pixel keys per official specs

        /// <summary>
        /// Initializes a new instance of the <see cref="HidComDriverStreamDeckModule6"/> class.
        /// </summary>
        public HidComDriverStreamDeckModule6()
            : base(ImageSize)
        {
        }

        // Module 6 specifications per official docs (override Mini's values where different)
        
        /// <inheritdoc/>
        public override int ExpectedFeatureReportLength => 32; // Feature reports are 32 bytes

        /// <inheritdoc/>
        public override int ExpectedInputReportLength => 65; // Input reports are 65 bytes

        /// <inheritdoc/>
        public override byte FirmwareVersionFeatureId => 0xA1; // AP2 (Primary firmware)

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
        public override void PrepareDataForTransmission(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast)
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
        public override byte[] GetBrightnessMessage(byte percent)
        {
            if (percent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent));
            }
            
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
        public override byte[] GetLogoMessage()
        {
            var buffer = new byte[32]; // Feature reports are 32 bytes, zero-padded
            buffer[0] = 0x0B; // Report ID
            buffer[1] = 0x63; // Command
            buffer[2] = 0x00; // Show Boot Logo
            
            return buffer;
        }
    }
}
