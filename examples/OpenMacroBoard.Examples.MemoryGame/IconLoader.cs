using OpenMacroBoard.SDK;
using System;
using System.Reflection;

namespace OpenMacroBoard.Examples.MemoryGame;

public static class IconLoader
{
    public static KeyBitmap LoadIconByName(string name, bool active)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"OpenMacroBoard.Examples.MemoryGame.icons.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException($"There is no icon with the name '{name}'.", nameof(name));

        // The SDK reads PNG files itself; transparent parts become black.
        var icon = KeyBitmap.Create.FromStream(stream);

        return active ? icon : Darken(icon);
    }

    private static KeyBitmap Darken(KeyBitmap icon)
    {
        // Key bitmaps hold their pixels as BGR with one byte per channel.
        var pixels = ((IKeyBitmapDataAccess)icon).GetData().ToArray();

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] /= 2;
        }

        return KeyBitmap.Create.FromBgr24Array(icon.Width, icon.Height, pixels);
    }
}
