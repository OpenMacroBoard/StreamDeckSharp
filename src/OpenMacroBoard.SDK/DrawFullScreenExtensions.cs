using OpenMacroBoard.SDK.Internals;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Extension method to generate fullscreen images on <see cref="IMacroBoard"/>s.
/// </summary>
public static class DrawFullScreenExtensions
{
    /// <summary>
    /// Draw a given image as fullscreen (spanning over all keys)
    /// </summary>
    /// <param name="board">The board the image should be drawn to.</param>
    /// <param name="image">The image that should be drawn.</param>
    /// <param name="resizeMode">The resize mode that should be used to fit the image.</param>
    /// <exception cref="ArgumentNullException">The provided board or bitmap is null.</exception>
    public static void DrawFullScreenBitmap
    (
        this IMacroBoard board,
        Image image,
        ResizeMode resizeMode = ResizeMode.BoxPad
    )
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(image);

        byte[] imgData;

        using (var ctx = ResizeToFullStreamDeckImage(image, board.Keys.Area.Size, resizeMode))
        {
            imgData = ctx.Item.ToBgr24PixelArray();
        }

        for (var i = 0; i < board.Keys.Count; i++)
        {
            var img = GetKeyImageFromFull(board.Keys[i], imgData, board.Keys.Area.Size);
            board.SetKeyBitmap(i, img);
        }
    }

    private static ConditionalDisposable<Image<Bgr24>> ResizeToFullStreamDeckImage
    (
        Image image,
        OmbSize newSize,
        ResizeMode resizeMode
    )
    {
        return ConstrainedContext.For(
            image,
            x =>
            {
                if (x is not Image<Bgr24> bgr24)
                {
                    return null;
                }

                if (x.Width != newSize.Width || x.Height != newSize.Height)
                {
                    return null;
                }

                return bgr24;
            },
            _ =>
            {
                var scaled = new Image<Bgr24>(image.Width, image.Height);

                var resizeOptions = new ResizeOptions()
                {
                    Mode = resizeMode,
                    Size = new(newSize.Width, newSize.Height),
                    Sampler = KnownResamplers.Welch,
                };

                scaled.Mutate(x =>
                {
                    x.DrawImage(image, 1);
                    x.Resize(resizeOptions);
                });

                return scaled;
            }
        );
    }

    private static KeyBitmap GetKeyImageFromFull
    (
        OmbRectangle keyPos,
        byte[] fullImageData,
        OmbSize fullImageSize
    )
    {
        var keyImgData = new byte[keyPos.Width * keyPos.Height * 3];
        var stride = 3 * fullImageSize.Width;

        for (var y = 0; y < keyPos.Height; y++)
        {
            for (var x = 0; x < keyPos.Width; x++)
            {
                var p = (keyPos.Top + y) * stride + (keyPos.Left + x) * 3;
                var kPos = (y * keyPos.Width + x) * 3;

                keyImgData[kPos + 0] = fullImageData[p + 0];
                keyImgData[kPos + 1] = fullImageData[p + 1];
                keyImgData[kPos + 2] = fullImageData[p + 2];
            }
        }

        return KeyBitmap.Create.FromBgr24Array(keyPos.Width, keyPos.Height, keyImgData);
    }
}
