using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Extension method to generate "full screen" images.
/// </summary>
public static class DrawFullScreenExtensions
{
    /// <summary>
    /// Draw a given image as full screen (spanning over all keys).
    /// </summary>
    /// <param name="board">The board the image should be drawn to.</param>
    /// <param name="bitmap">The image that should be drawn.</param>
    /// <param name="resizeMode">How the image is fitted into the area of all keys.</param>
    public static void DrawFullScreenBitmap
    (
        this IMacroBoard board,
        KeyBitmap bitmap,
        OmbResizeMode resizeMode = OmbResizeMode.BoxPad
    )
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(bitmap);

        var fullSize = board.Keys.Area.Size;
        var fullImage = bitmap.Resize(fullSize.Width, fullSize.Height, resizeMode);
        var fullImageData = ((IKeyBitmapDataAccess)fullImage).GetData().Span;

        for (var i = 0; i < board.Keys.Count; i++)
        {
            var img = GetKeyImageFromFull(board.Keys[i], fullImageData, fullSize);
            board.SetKeyBitmap(i, img);
        }
    }

    private static KeyBitmap GetKeyImageFromFull
    (
        OmbRectangle keyPos,
        ReadOnlySpan<byte> fullImageData,
        OmbSize fullImageSize
    )
    {
        var keyImgData = new byte[keyPos.Width * keyPos.Height * 3];
        var stride = 3 * fullImageSize.Width;

        for (var y = 0; y < keyPos.Height; y++)
        {
            var row = fullImageData.Slice(((keyPos.Top + y) * stride) + (keyPos.Left * 3), keyPos.Width * 3);
            row.CopyTo(keyImgData.AsSpan(y * keyPos.Width * 3));
        }

        return new KeyBitmap(keyPos.Width, keyPos.Height, keyImgData);
    }
}
