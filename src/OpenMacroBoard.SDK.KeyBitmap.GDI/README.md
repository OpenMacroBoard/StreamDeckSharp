# OpenMacroBoard.SDK.KeyBitmap.GDI

Draw the images for the keys of a macro board with GDI+ (`System.Drawing`): text, lines,
shapes, everything `Graphics` offers. This package adds two methods to `KeyBitmap.Create` of
[OpenMacroBoard.SDK](../OpenMacroBoard.SDK).

**Windows only**, because `System.Drawing` itself only works on Windows.

## Draw on a key

`FromGraphics` gives you a `Graphics` object of the size you ask for and turns what you drew
into a `KeyBitmap`.

```csharp
using OpenMacroBoard.SDK;
using System.Drawing;

using var font = new Font("Arial", 20);

var key = KeyBitmap.Create.FromGraphics(72, 72, g =>
{
    g.Clear(Color.DarkBlue);
    g.DrawString("Hi!", font, Brushes.White, 10, 20);
});

board.SetKeyBitmap(0, key);
```

## Use an existing bitmap

`FromBitmap` converts a `System.Drawing.Bitmap` you already have.

```csharp
using var bitmap = new Bitmap("icon.png");
var key = KeyBitmap.Create.FromBitmap(bitmap);
```

The bitmap has to be in the pixel format `Format24bppRgb` or `Format32bppArgb`. Transparent
parts become black.

## Do you need this package?

Not for loading image files. The SDK reads BMP, PNG and JPEG files on every platform:

```csharp
var key = KeyBitmap.Create.FromFile("icon.png");
```

You need it when you want to draw at run time and GDI+ is your drawing library. For drawing
on Linux and macOS too, see [OpenMacroBoard.SDK.KeyBitmap.SkiaSharp](../OpenMacroBoard.SDK.KeyBitmap.SkiaSharp).
