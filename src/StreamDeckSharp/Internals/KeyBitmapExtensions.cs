using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace StreamDeckSharp.Internals
{
    internal static class KeyBitmapExtensions
    {
        public static ReadOnlySpan<byte> GetScaledVersion(this KeyBitmap keyBitmap, int width, int height)
        {
            IKeyBitmapDataAccess keyDataAccess = keyBitmap;

            if (keyDataAccess.IsEmpty)
            {
                // default span is of length 0 (the caller has to check this special case)
                return default;
            }

            var underlyingData = keyDataAccess.GetData();

            if (keyBitmap.Width == width && keyBitmap.Height == height)
            {
                // if it is already the size we need just return the underlying data
                return underlyingData;
            }

            using var image = Image.LoadPixelData<Bgr24>(underlyingData, keyBitmap.Width, keyBitmap.Height);

            image.Mutate(x => x.Resize(width, height));

            var scaledPixelData = image.ToBgr24PixelArray();

            return new ReadOnlySpan<byte>(scaledPixelData);
        }
    }
}
