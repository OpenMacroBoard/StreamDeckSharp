using System.Collections.Generic;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A <see cref="IMacroBoard"/> adapter that replays brightness and key bitmaps if a device is disconnected.
/// </summary>
public class DisconnectReplayAdapter : MacroBoardAdapter
{
    private readonly Dictionary<int, KeyBitmap> mostRecentKeyBitmaps = [];
    private byte? mostRecentBrightness = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisconnectReplayAdapter"/> class.
    /// </summary>
    public DisconnectReplayAdapter(IMacroBoard macroBoard)
        : base(macroBoard)
    {
        ConnectionStateChanged += ReplayEventsForConnectionStateChange;
    }

    /// <inheritdoc/>
    public override void SetBrightness(byte percent)
    {
        mostRecentBrightness = percent;
        base.SetBrightness(percent);
    }

    /// <inheritdoc/>
    public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        mostRecentKeyBitmaps[keyId] = bitmapData;
        base.SetKeyBitmap(keyId, bitmapData);
    }

    private void ReplayEventsForConnectionStateChange(object? sender, ConnectionEventArgs e)
    {
        if (e.NewConnectionState)
        {
            // devices connected again: replay last known values

            if (mostRecentBrightness is not null)
            {
                base.SetBrightness(mostRecentBrightness.Value);
            }

            foreach (var bmp in mostRecentKeyBitmaps)
            {
                base.SetKeyBitmap(bmp.Key, bmp.Value);
            }
        }
    }
}
