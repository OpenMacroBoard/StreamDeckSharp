using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// An interface that allows you to interact with (LCD) macro boards
    /// </summary>
    public interface IMacroBoard : IDisposable
    {
        /// <summary>
        /// Informations about the keys and their position
        /// </summary>
        IKeyPositionCollection Keys { get; }

        /// <summary>
        /// Is raised when a key is pressed
        /// </summary>
        event EventHandler<KeyEventArgs> KeyStateChanged;

        /// <summary>
        /// Gets a value indicating whether the MarcoBoard is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Is raised when the MarcoBoard is beeing disconnected or connected
        /// </summary>
        event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Sets the brightness for this <see cref="IMacroBoard"/>
        /// </summary>
        /// <param name="percent">Brightness in percent (0 - 100)</param>
        /// <remarks>
        /// The brightness on the device is controlled with PWM (https://en.wikipedia.org/wiki/Pulse-width_modulation).
        /// This results in a non-linear correlation between set percentage and perceived brightness.
        /// 
        /// In a nutshell: changing from 10 - 30 results in a bigger change than 80 - 100 (barely visible change)
        /// This effect should be compensated outside this library
        /// </remarks>
        void SetBrightness(byte percent);

        /// <summary>
        /// Sets a background image for a given key
        /// </summary>
        /// <param name="keyId">Specifies which key the image will be applied on</param>
        /// <param name="bitmapData">Bitmap. The key will be painted black if this value is null.</param>
        void SetKeyBitmap(int keyId, KeyBitmap bitmapData);

        /// <summary>
        /// Shows the standby logo (Fullscreen)
        /// </summary>
        void ShowLogo();
    }
}
