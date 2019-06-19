using OpenMacroBoard.SDK;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckXlHardwareInfo
        : IHardwareInternalInfos
    {
        private const int imgSize = 96;

        public int HeaderSize => 8;

        public int ReportSize => 1024;

        public int KeyReportOffset => 3;

        public int UsbVendorId => Hardware.VendorIds.ElgatoSystemsGmbH;

        public int UsbProductId => Hardware.ProductIds.StreamDeckXL;

        public GridKeyPositionCollection Keys
            => keyPositions;

        private static readonly GridKeyPositionCollection keyPositions;
        private static byte[] cachedNullImage = null;

        static StreamDeckXlHardwareInfo()
        {
            keyPositions = new GridKeyPositionCollection(8, 4, imgSize, 25);
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

        private static byte[] GetNullImage()
        {
            if (cachedNullImage is null)
            {
                var rawNullImg = new KeyBitmap(1, 1, new byte[] { 0, 0, 0 }).GetScaledVersion(imgSize, imgSize);
                cachedNullImage = EncodeImageToJpg(rawNullImg);
            }

            return cachedNullImage;
        }

        private static byte[] EncodeImageToJpg(byte[] rgb24)
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

            var enc = new JpegBitmapEncoder() { QualityLevel = 100 };
            var f = BitmapSource.Create(imgSize, imgSize, 96, 96, PixelFormats.Bgr32, null, data, stride);
            enc.Frames.Add(BitmapFrame.Create(f));

            using (var ms = new MemoryStream())
            {
                enc.Save(ms);
                var jpgBytes = ms.ToArray();
                return jpgBytes;
            }
        }
    }
}
