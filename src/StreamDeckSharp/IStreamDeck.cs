using System;

namespace StreamDeckSharp
{
    /// <summary>
    /// Bare minimum StreamDeck Interface.
    /// </summary>
    public interface IStreamDeck : IDisposable
    {
        /// <summary>
        /// The number of keys present on the Stream Deck
        /// </summary>
        /// <remarks>
        /// At the moment there is only a Stream Deck device with 5x3 keys.
        /// But this may change in the future so please use this property in your
        /// code / for-loops.
        /// </remarks>
        int KeyCount { get; }

        /// <summary>
        /// Is raised when a key is pressed
        /// </summary>
        event EventHandler<KeyEventArgs> KeyStateChanged;

        /// <summary>
        /// Sets the brightness for this <see cref="IStreamDeck"/>
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
        /// Shows the Stream Deck logo (Fullscreen)
        /// </summary>
        void ShowLogo();

        /// <summary>
        /// Size of the icon in pixels
        /// </summary>
        int IconSize { get; }

        /// <summary>
        /// Gets a value indicating whether the StreamDeck is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Is raised when the StreamDeck is beeing disconnected or connected
        /// </summary>
        event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
    }
}
