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

        private const int pagePacketSize = 8191;
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

        public static byte[] GeneratePage1(int keyId, byte[] imgData)
        {
            var p1 = new byte[pagePacketSize];
            Array.Copy(headerTemplatePage1, p1, headerTemplatePage1.Length);

            if (imgData != null)
                Array.Copy(imgData, 0, p1, headerTemplatePage1.Length, numFirstPagePixels * 3);

            p1[5] = (byte)(keyId + 1);
            return p1;
        }

        public static byte[] GeneratePage2(int keyId, byte[] imgData)
        {
            var p2 = new byte[pagePacketSize];
            Array.Copy(headerTemplatePage2, p2, headerTemplatePage2.Length);

            if (imgData != null)
                Array.Copy(imgData, numFirstPagePixels * 3, p2, headerTemplatePage2.Length, numSecondPagePixels * 3);

            p2[5] = (byte)(keyId + 1);
            return p2;
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
