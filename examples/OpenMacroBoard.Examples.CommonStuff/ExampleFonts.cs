using SkiaSharp;
using System;
using System.Linq;

namespace OpenMacroBoard.Examples.CommonStuff;

/// <summary>
/// Picks the font for the examples. Not every system has Arial (most Linux systems do not),
/// so this tries a few similar fonts and then falls back to any installed font.
/// </summary>
public static class ExampleFonts
{
    private static readonly string[] PreferredFamilies =
    [
        "Arial",
        "Helvetica Neue",
        "Helvetica",
        "Liberation Sans",
        "Arimo",
        "DejaVu Sans",
        "Noto Sans",
        "Segoe UI",
        "Roboto",
        "Ubuntu",
        "Cantarell",
        "FreeSans",
    ];

    private static readonly Lazy<SKTypeface> Regular = new(() => FindTypeface(SKFontStyle.Normal));
    private static readonly Lazy<SKTypeface> Bold = new(() => FindTypeface(SKFontStyle.Bold));

    /// <summary>
    /// Creates a font with the given size in pixels.
    /// </summary>
    public static SKFont Create(float size, bool bold = false)
    {
        return new SKFont(bold ? Bold.Value : Regular.Value, size);
    }

    private static SKTypeface FindTypeface(SKFontStyle style)
    {
        var fontManager = SKFontManager.Default;

        // A fallback font may not have every style (for example no bold variant),
        // the font manager then returns the closest one it has.
        var typeface = PreferredFamilies
            .Concat(fontManager.FontFamilies.Order(StringComparer.Ordinal))
            .Where(family => !string.IsNullOrEmpty(family))
            .Select(family => fontManager.MatchFamily(family, style))
            .FirstOrDefault(candidate => candidate is not null && candidate.GlyphCount > 0);

        return typeface ?? throw new InvalidOperationException(
            "This example draws text and needs a font, but no font is installed on this system. "
            + "Install any TrueType or OpenType font (for example the package 'fonts-dejavu' or 'fonts-liberation' on Debian and Ubuntu)."
        );
    }
}
