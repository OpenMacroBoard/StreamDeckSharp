using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// An interface that allows you to interact with (LCD) macro boards
/// </summary>
public interface IMacroBoard : IDisposable
{
    /// <summary>
    /// Is raised when a key is pressed
    /// </summary>
    event EventHandler<KeyEventArgs> KeyStateChanged;

    /// <summary>
    /// Is raised when the MarcoBoard is being disconnected or connected
    /// </summary>
    event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

    /// <summary>
    /// Informations about the keys and their position
    /// </summary>
    IKeyLayout Keys { get; }

    /// <summary>
    /// Gets a value indicating whether the MarcoBoard is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Sets the brightness for this <see cref="IMacroBoard"/>
    /// </summary>
    /// <param name="percent">Brightness in percent (0 - 100)</param>
    /// <remarks>
    /// <para>
    /// The brightness on the device is controlled with PWM (https://en.wikipedia.org/wiki/Pulse-width_modulation).
    /// This results in a non-linear correlation between set percentage and perceived brightness.
    /// </para>
    /// <para>
    /// In a nutshell: changing from 10 - 30 results in a bigger change than 80 - 100 (barely visible change)
    /// This effect should be compensated outside this library
    /// </para>
    /// </remarks>
    void SetBrightness(byte percent);

    /// <summary>
    /// Sets a background image for a given key
    /// </summary>
    /// <param name="keyId">Specifies which key the image will be applied on</param>
    /// <param name="bitmapData">Bitmap. The key will be painted black if this value is null.</param>
    void SetKeyBitmap(int keyId, KeyBitmap bitmapData);

    /// <summary>
    /// Shows the standby logo (full-screen)
    /// </summary>
    void ShowLogo();

    /// <summary>
    /// Gets the firmware version.
    /// </summary>
    /// <returns>
    /// Returns the firmware version
    /// or <see cref="string.Empty"/> if the device doesn't have a firmware.
    /// </returns>
    string GetFirmwareVersion();

    /// <summary>
    /// Gets the serial number.
    /// </summary>
    /// <returns>
    /// Returns the serial number
    /// or <see cref="string.Empty"/> if the device doesn't have a serial number.
    /// </returns>
    string GetSerialNumber();
}
