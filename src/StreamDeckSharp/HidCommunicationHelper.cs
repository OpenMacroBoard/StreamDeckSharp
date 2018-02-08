using System;

//Special thanks to Lange (Alex Van Camp) - https://github.com/Lange/node-elgato-stream-deck
//The node-js implementation was the basis of this .NET C# implementation

namespace StreamDeckSharp
{
    /// <summary>
    /// Internal StreamDeck HID communication class
    /// </summary>
    internal static class HidCommunicationHelper
    {
        public const int VendorId = 0x0fd9;    //Elgato Systems GmbH
        public const int ProductId = 0x0060;   //Stream Deck
        public const int PagePacketSize = 8191;

        private const int numFirstPagePixels = 2583;
        private const int numSecondPagePixels = 2601;

        private static readonly byte[] headerTemplatePage1 = new byte[] {
            0x02, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x42, 0x4d, 0xf6, 0x3c, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xc0, 0x3c, 0x00, 0x00, 0xc4, 0x0e,
            0x00, 0x00, 0xc4, 0x0e, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private static readonly byte[] headerTemplatePage2 = new byte[] {
            0x02, 0x01, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public static void GeneratePage1(byte[] buffer, int keyId, byte[] imgData)
        {
            Array.Copy(headerTemplatePage1, buffer, headerTemplatePage1.Length);
            Array.Copy(imgData, 0, buffer, headerTemplatePage1.Length, numFirstPagePixels * 3);
            buffer[5] = (byte)(keyId + 1);
        }

        public static void GeneratePage2(byte[] buffer, int keyId, byte[] imgData)
        {
            Array.Copy(headerTemplatePage2, buffer, headerTemplatePage2.Length);
            Array.Copy(imgData, numFirstPagePixels * 3, buffer, headerTemplatePage2.Length, numSecondPagePixels * 3);
            buffer[5] = (byte)(keyId + 1);
        }

        public static byte[] GetBrightnessMsg(byte percent)
        {
            if (percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            var buffer = new byte[] { 0x05, 0x55, 0xaa, 0xd1, 0x01, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            buffer[5] = percent;
            return buffer;
        }

        public static readonly byte[] ShowLogoMsg = new byte[] { 0x0B, 0x63 };
    }

}
