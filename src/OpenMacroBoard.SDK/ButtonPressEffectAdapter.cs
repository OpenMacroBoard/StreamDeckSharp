using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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

    private KeyBitmap ResizeBitmap(KeyBitmap keyBitmap)
    {
        var bitmapDataAccess = (IKeyBitmapDataAccess)keyBitmap;

        var targetWidth = (int)Math.Round(Config.Scale * keyBitmap.Width);
        var targetHeight = (int)Math.Round(Config.Scale * keyBitmap.Height);

        var smallerImage = bitmapDataAccess.ToImage();
        smallerImage.Mutate(x => x.Resize(targetWidth, targetHeight));

        var offsetLeft = (int)Math.Round((keyBitmap.Width - targetWidth) * Config.OriginX);
        var offsetTop = (int)Math.Round((keyBitmap.Height - targetHeight) * Config.OriginY);

        var color = Color.FromRgb(
            Config.BackgroundColor.R,
            Config.BackgroundColor.G,
            Config.BackgroundColor.B
        );

        var newImage = new Image<Bgr24>(keyBitmap.Width, keyBitmap.Height);

        newImage.Mutate(x =>
        {
            x.BackgroundColor(color);
            x.DrawImage(smallerImage, new Point(offsetLeft, offsetTop), 1);
        });

        return KeyBitmap.Create.FromImageSharpImage(newImage);
    }
}
