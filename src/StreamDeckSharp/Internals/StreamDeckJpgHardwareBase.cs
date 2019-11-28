using OpenMacroBoard.SDK;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal abstract class StreamDeckJpgHardwareBase
        : IHardwareInternalInfos
    {
        private readonly int imgSize;
        public abstract string DeviceName { get; }
        public abstract int UsbProductId { get; }

        public int HeaderSize => 8;
        public int ReportSize => 1024;
        public int KeyReportOffset => 4;
        public int UsbVendorId => VendorIds.ElgatoSystemsGmbH;

        public int KeyCooldown => 25;
        public byte FirmwareVersionFeatureId => 5;
        public byte SerialNumberFeatureId => 6;
        public int FirmwareReportSkip => 6;
        public int SerialNumberReportSkip => 2;

        public GridKeyPositionCollection Keys { get; }
        private static byte[] cachedNullImage = null;

        private readonly ImageCodecInfo jpgEncoder;
        private readonly EncoderParameters jpgParams;

        public StreamDeckJpgHardwareBase(GridKeyPositionCollection keyPositions)
        {
            jpgEncoder = ImageCodecInfo.GetImageDecoders().Where(d => d.FormatID == ImageFormat.Jpeg.Guid).First();
            jpgParams = new EncoderParameters(1);
            jpgParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

            Keys = keyPositions;
            imgSize = keyPositions.KeyWidth;
        }

        public int ExtKeyIdToHardwareKeyId(int extKeyId)
            => extKeyId;

        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(imgSize, imgSize);

            if (rawData is null)
                return GetNullImage();

            return EncodeImageToJpg(rawData);
        }

        public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
            => hardwareKeyId;

        public void PrepareDataForTransmittion(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast)
        {
            data[0] = 2;
            data[1] = 7;
            data[2] = (byte)keyId;
            data[3] = (byte)(isLast ? 1 : 0);
            data[4] = (byte)(payloadLength & 255);
            data[5] = (byte)(payloadLength >> 8);
            data[6] = (byte)pageNumber;
        }

        private byte[] GetNullImage()
        {
            if (cachedNullImage is null)
            {
                var rawNullImg = new KeyBitmap(1, 1, new byte[] { 0, 0, 0 }).GetScaledVersion(imgSize, imgSize);
                cachedNullImage = EncodeImageToJpg(rawNullImg);
            }

            return cachedNullImage;
        }

        private byte[] EncodeImageToJpg(byte[] rgb24)
        {
            int stride = imgSize * 4;
            var data = new byte[imgSize * stride];

            for (int y = 0; y < imgSize; y++)
                for (int x = 0; x < imgSize; x++)
                {
                    var x1 = imgSize - 1 - x;
                    var y1 = imgSize - 1 - y;

                    var pTarget = (y * imgSize + x) * 4;
                    var pSource = (y1 * imgSize + x1) * 3;

                    data[pTarget + 0] = rgb24[pSource + 0];
                    data[pTarget + 1] = rgb24[pSource + 1];
                    data[pTarget + 2] = rgb24[pSource + 2];
                }

            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var pointer = pinnedArray.AddrOfPinnedObject();

                using (Bitmap target = new Bitmap(imgSize, imgSize, stride, System.Drawing.Imaging.PixelFormat.Format32bppRgb, pointer))
                {
                    using (var memStream = new MemoryStream())
                    {
                        target.Save(memStream, jpgEncoder, jpgParams);
                        return memStream.ToArray();
                    }
                }
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        public byte[] GetBrightnessMessage(byte percent)
        {
            if (percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent));

            var buffer = new byte[] { 0x03, 0x08, 0x64, 0x23, 0xB8, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA5, 0x49, 0xCD, 0x02, 0xFE, 0x7F, 0x00, 0x00 };
            buffer[2] = percent;
            buffer[3] = 0x23;  //0x23, sometimes 0x27

            return buffer;
        }

        public byte[] GetLogoMessage()
        {
            return new byte[] { 0x03, 0x02 };
        }
    }
}
