namespace StreamDeckSharp
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// The <see cref="IStreamDeck"/> interface is pretty basic to simplify implementation.
    /// This extension class adds some commonly used functions to make things simpler.
    /// </remarks>
    public static class InterfaceExtensions
    {
        /// <summary>
        /// Sets a background image for all keys
        /// </summary>
        /// <param name="deck"></param>
        /// <param name="bitmap"></param>
        public static void SetKeyBitmap(this IStreamDeck deck, KeyBitmap bitmap)
        {
            for (int i = 0; i < deck.KeyCount; i++)
                deck.SetKeyBitmap(i, bitmap);
        }

        /// <summary>
        /// Sets background to black for a given key
        /// </summary>
        /// <param name="deck"></param>
        /// <param name="keyId"></param>
        public static void ClearKey(this IStreamDeck deck, int keyId)
        {
            deck.SetKeyBitmap(keyId, KeyBitmap.Black);
        }

        /// <summary>
        /// Sets background to black for all given keys
        /// </summary>
        /// <param name="deck"></param>
        public static void ClearKeys(this IStreamDeck deck)
        {
            deck.SetKeyBitmap(KeyBitmap.Black);
        }
    }
}
