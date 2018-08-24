namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// A bunch of extensions to clear all keys, or set a single <see cref="KeyBitmap"/> to all keys.
    /// </summary>
    public static class SetKeyExtensions
    {
        /// <summary>
        /// Sets a background image for all keys
        /// </summary>
        /// <param name="board"></param>
        /// <param name="bitmap"></param>
        public static void SetKeyBitmap(this IMacroBoard board, KeyBitmap bitmap)
        {
            for (int i = 0; i < board.Keys.Count; i++)
                board.SetKeyBitmap(i, bitmap);
        }

        /// <summary>
        /// Sets background to black for a given key
        /// </summary>
        /// <param name="board"></param>
        /// <param name="keyId"></param>
        public static void ClearKey(this IMacroBoard board, int keyId)
        {
            board.SetKeyBitmap(keyId, KeyBitmap.Black);
        }

        /// <summary>
        /// Sets background to black for all given keys
        /// </summary>
        /// <param name="board"></param>
        public static void ClearKeys(this IMacroBoard board)
        {
            board.SetKeyBitmap(KeyBitmap.Black);
        }
    }
}
