using System;
using System.Collections.Generic;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Macro board adapter that implements a software button press effect.
/// </summary>
/// <remarks>
/// <para>This vaguely mimics the perspective of a real button being pushed
/// and provides better feedback to the user that a button push was registered.</para>
/// </remarks>
public class ButtonPressEffectAdapter : MacroBoardAdapter
{
    private readonly Dictionary<int, KeyBitmap> mostRecentKeyBitmaps = [];
    private readonly Dictionary<int, bool> keyPressedState = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonPressEffectAdapter"/> class.
    /// </summary>
    /// <param name="macroBoard">The <see cref="IMacroBoard"/> this effect should be applied to.</param>
    public ButtonPressEffectAdapter(IMacroBoard macroBoard)
        : this(macroBoard, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ButtonPressEffectAdapter"/> class.
    /// </summary>
    /// <param name="macroBoard">The board that is wrapped with the button press effect.</param>
    /// <param name="config">The configuration that should be used. If null the default configuration will be used.</param>
    public ButtonPressEffectAdapter(IMacroBoard macroBoard, ButtonPressEffectConfig? config)
        : base(macroBoard)
    {
        KeyStateChanged += SoftwareButtonFeature_KeyStateChanged;
        Config = config ?? new ButtonPressEffectConfig();
    }

    /// <summary>
    /// The configuration that controls the behavior of the button press effect feature.
    /// </summary>
    public ButtonPressEffectConfig Config { get; }

    /// <inheritdoc/>
    public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        mostRecentKeyBitmaps[keyId] = bitmapData;
        UpdateKeyBitmap(keyId);
    }

    private void SoftwareButtonFeature_KeyStateChanged(object? sender, KeyEventArgs e)
    {
        keyPressedState[e.Key] = e.IsDown;
        UpdateKeyBitmap(e.Key);
    }

    private void UpdateKeyBitmap(int keyId)
    {
        var bitmap = GetBitmapForKey(keyId);

        if (bitmap != null)
        {
            base.SetKeyBitmap(keyId, bitmap);
        }
    }

    private bool IsKeyPressed(int keyId)
    {
        if (keyPressedState.TryGetValue(keyId, out var keyPressed))
        {
            return keyPressed;
        }

        return false;
    }

    private KeyBitmap? GetBitmapForKey(int keyId)
    {
        if (!mostRecentKeyBitmaps.TryGetValue(keyId, out var bitmap))
        {
            return null;
        }

        return IsKeyPressed(keyId) ? ResizeBitmap(bitmap) : bitmap;
    }

    /// <summary>
    /// Draws a smaller version of the bitmap on the background color.
    /// </summary>
    private KeyBitmap ResizeBitmap(KeyBitmap keyBitmap)
    {
        const int channelCount = 3;

        var width = keyBitmap.Width;
        var height = keyBitmap.Height;

        var targetWidth = (int)Math.Round(Config.Scale * width);
        var targetHeight = (int)Math.Round(Config.Scale * height);

        var offsetLeft = (int)Math.Round((width - targetWidth) * Config.OriginX);
        var offsetTop = (int)Math.Round((height - targetHeight) * Config.OriginY);

        var result = new byte[width * height * channelCount];

        for (var i = 0; i < result.Length; i += channelCount)
        {
            result[i] = Config.BackgroundColor.B;
            result[i + 1] = Config.BackgroundColor.G;
            result[i + 2] = Config.BackgroundColor.R;
        }

        if (targetWidth >= 1 && targetHeight >= 1)
        {
            var smaller = ((IKeyBitmapDataAccess)keyBitmap.Resize(targetWidth, targetHeight)).GetData().Span;

            // Only the part that lies inside of the key is copied.
            var firstX = Math.Max(0, -offsetLeft);
            var lastX = Math.Min(targetWidth, width - offsetLeft);

            for (var y = Math.Max(0, -offsetTop); y < Math.Min(targetHeight, height - offsetTop); y++)
            {
                if (lastX <= firstX)
                {
                    break;
                }

                var from = ((y * targetWidth) + firstX) * channelCount;
                var to = (((y + offsetTop) * width) + firstX + offsetLeft) * channelCount;
                var length = (lastX - firstX) * channelCount;

                smaller.Slice(from, length).CopyTo(result.AsSpan(to, length));
            }
        }

        return new KeyBitmap(width, height, result);
    }
}
