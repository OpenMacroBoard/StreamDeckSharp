using OpenMacroBoard.SDK;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// A basic factory extension to create <see cref="KeyBitmap"/>s
    /// </summary>
    public static class KeyBitmapBasicExtensions
    {
        /// <summary>
        /// Creates a single color (single pixel) <see cref="KeyBitmap"/> with a given color.
        /// </summary>
        /// <param name="builder">The builder that is used to create the <see cref="KeyBitmap"/></param>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        /// <returns></returns>
        public static KeyBitmap FromRgb(this IKeyBitmapFactory builder, byte r, byte g, byte b)
        {
            //If everything is 0 (black) take a shortcut ;-)
            if (r == 0 && g == 0 && b == 0)
                return KeyBitmap.Black;

            var buffer = new byte[3] { b, g, r };
            return new KeyBitmap(1, 1, buffer);
        }
    }
}
