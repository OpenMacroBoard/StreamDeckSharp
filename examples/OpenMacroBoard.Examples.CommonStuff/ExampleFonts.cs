using SixLabors.Fonts;
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

    private static readonly Lazy<FontFamily> Family = new(FindFamily);

    public static Font Create(float size, FontStyle style = FontStyle.Regular)
    {
        var family = Family.Value;

        // A fallback font may not have every style (for example no bold variant).
        if (!family.GetAvailableStyles().Contains(style))
        {
            style = family.GetAvailableStyles().First();
        }

        return family.CreateFont(size, style);
    }

    private static FontFamily FindFamily()
    {
        foreach (var name in PreferredFamilies)
        {
            if (SystemFonts.TryGet(name, out var family))
            {
                return family;
            }
        }

        var installed = SystemFonts.Families.OrderBy(f => f.Name, StringComparer.Ordinal).ToList();

        if (installed.Count == 0)
        {
            throw new InvalidOperationException(
                "This example draws text and needs a font, but no font is installed on this system. "
                + "Install any TrueType or OpenType font (for example the package 'fonts-dejavu' or 'fonts-liberation' on Debian and Ubuntu)."
            );
        }

        return installed[0];
    }
}
