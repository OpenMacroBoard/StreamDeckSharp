using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;

namespace StreamDeckSharp.Internals
{
    /// <summary>
    /// HID Stream Deck communication driver for JPEG based devices.
    /// </summary>
    public sealed class HidComDriverStreamDeckJpeg
        : IStreamDeckHidComDriver
    {
        private readonly int imgSize;
        private readonly JpegEncoder jpgEncoder;

        private byte[] cachedNullImage = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="HidComDriverStreamDeckJpeg"/> class.
        /// </summary>
        /// <param name="imgSize">The size of the button images in pixels.</param>
        /// /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="imgSize"/> is smaller than one.</exception>
        public HidComDriverStreamDeckJpeg(int imgSize)
        {
            if (imgSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(imgSize));
            }

            jpgEncoder = new JpegEncoder()
            {
                Quality = 100,
            };

            this.imgSize = imgSize;
        }

        /// <inheritdoc/>
        public int HeaderSize => 8;

        /// <inheritdoc/>
        public int ReportSize => 1024;

        /// <inheritdoc/>
        public int ExpectedFeatureReportLength => 32;

        /// <inheritdoc/>
        public int ExpectedOutputReportLength => 1024;

        /// <inheritdoc/>
        public int ExpectedInputReportLength => 512;

        /// <inheritdoc/>
        public int KeyReportOffset => 4;

        /// <inheritdoc/>
        public byte FirmwareVersionFeatureId => 5;

        /// <inheritdoc/>
        public byte SerialNumberFeatureId => 6;

        /// <inheritdoc/>
        public int FirmwareVersionReportSkip => 6;

        /// <inheritdoc/>
        public int SerialNumberReportSkip => 2;

        /// <inheritdoc/>
        public double BytesPerSecondLimit { get; set; } = double.PositiveInfinity;

        /// <inheritdoc/>
        public int ExtKeyIdToHardwareKeyId(int extKeyId)
        {
            return extKeyId;
        }

        /// <inheritdoc/>
        public byte[] GeneratePayload(KeyBitmap keyBitmap)
        {
            var rawData = keyBitmap.GetScaledVersion(imgSize, imgSize);

            if (rawData.Length == 0)
            {
                return GetNullImage();
            }

            return EncodeImageToJpg(rawData);
        }

        /// <inheritdoc/>
        public int HardwareKeyIdToExtKeyId(int hardwareKeyId)
        {
            return hardwareKeyId;
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
            data[0] = 2;
            data[1] = 7;
            data[2] = (byte)keyId;
            data[3] = (byte)(isLast ? 1 : 0);
            data[4] = (byte)(payloadLength & 255);
            data[5] = (byte)(payloadLength >> 8);
            data[6] = (byte)pageNumber;
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
                0x03, 0x08, 0x64, 0x23, 0xB8, 0x01, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xA5, 0x49, 0xCD, 0x02, 0xFE, 0x7F, 0x00, 0x00,
            };

            buffer[2] = percent;
            buffer[3] = 0x23;  // 0x23, sometimes 0x27

            return buffer;
        }

        /// <inheritdoc/>
        public byte[] GetLogoMessage()
        {
            return new byte[] { 0x03, 0x02 };
        }

        private byte[] GetNullImage()
        {
            if (cachedNullImage is null)
            {
                var rawNullImg = KeyBitmap.Create.FromBgr24Array(1, 1, new byte[] { 0, 0, 0 }).GetScaledVersion(imgSize, imgSize);
                cachedNullImage = EncodeImageToJpg(rawNullImg);
            }

            return cachedNullImage;
        }

        private byte[] EncodeImageToJpg(ReadOnlySpan<byte> bgr24)
        {
            // Flip XY ... for some reason the JPEG devices have flipped x and y coordinates.
            var flippedData = new byte[imgSize * imgSize * 3];

            for (var y = 0; y < imgSize; y++)
            {
                for (var x = 0; x < imgSize; x++)
                {
                    var x1 = imgSize - 1 - x;
                    var y1 = imgSize - 1 - y;

                    var pTarget = (y * imgSize + x) * 3;
                    var pSource = (y1 * imgSize + x1) * 3;

                    flippedData[pTarget + 0] = bgr24[pSource + 0];
                    flippedData[pTarget + 1] = bgr24[pSource + 1];
                    flippedData[pTarget + 2] = bgr24[pSource + 2];
                }
            }

            using var image = Image.LoadPixelData<Bgr24>(flippedData, imgSize, imgSize);

            using var memStream = new MemoryStream();
            image.SaveAsJpeg(memStream, jpgEncoder);

            return memStream.ToArray();
        }
    }
}
