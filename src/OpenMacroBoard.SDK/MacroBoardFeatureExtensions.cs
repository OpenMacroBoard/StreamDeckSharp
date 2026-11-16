using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Extensions for <see cref="IMacroBoard"/> enrichment ;-)
/// </summary>
public static class MacroBoardFeatureExtensions
{
    /// <summary>
    /// Wraps an <see cref="IMacroBoard"/> with an button press effect adapter.
    /// </summary>
    /// <param name="macroBoard">The board that should be wrapped.</param>
    /// <param name="config">The configuration that should be used. The config is stored by reference, so later changes are also possible.</param>
    /// <returns>Returns a new board that implements the button press effect.</returns>
    /// <exception cref="ArgumentNullException">The provided board is null.</exception>
    public static IMacroBoard WithButtonPressEffect(this IMacroBoard macroBoard, ButtonPressEffectConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(macroBoard);
        return new ButtonPressEffectAdapter(macroBoard, config);
    }

    /// <summary>
    /// Wraps an <see cref="IMacroBoard"/> with a disconnect replay adapter.
    /// </summary>
    /// <param name="macroBoard">The board that should be wrapped.</param>
    /// <returns>Returns a new board that implements the replay feature.</returns>
    /// <remarks>
    /// <para>This adapter makes sure, that if a device is disconnected that previously set properties like
    /// images and brightness are replayed if the device is connected again.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">The provided board is null.</exception>
    public static IMacroBoard WithDisconnectReplay(this IMacroBoard macroBoard)
    {
        ArgumentNullException.ThrowIfNull(macroBoard);
        return new DisconnectReplayAdapter(macroBoard);
    }
}
