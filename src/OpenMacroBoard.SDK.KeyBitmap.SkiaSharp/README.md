# OpenMacroBoard.SDK.KeyBitmap.SkiaSharp

Draw the images for the keys of a macro board with [SkiaSharp](https://github.com/mono/SkiaSharp):
text, lines, shapes, everything an `SKCanvas` offers. This package adds two methods to
`KeyBitmap.Create` of [OpenMacroBoard.SDK](../OpenMacroBoard.SDK).

It works on Windows, Linux and macOS.

## Draw on a key

`FromSkiaCanvas` gives you a canvas of the size you ask for and turns what you drew into a
`KeyBitmap`. The canvas starts black.

```csharp
using OpenMacroBoard.SDK;
using SkiaSharp;

using var font = new SKFont(SKTypeface.Default, 24);
using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };

var key = KeyBitmap.Create.FromSkiaCanvas(72, 72, canvas =>
{
    canvas.Clear(SKColors.DarkBlue);
    canvas.DrawText("Hi!", 14, 44, SKTextAlign.Left, font, paint);
});

board.SetKeyBitmap(0, key);
```

## Use an existing bitmap

`FromSkiaBitmap` converts an `SKBitmap` you already have, in any color type. Transparent
parts become black.

```csharp
using var bitmap = SKBitmap.Decode("icon.webp");
var key = KeyBitmap.Create.FromSkiaBitmap(bitmap);
```

## Linux

SkiaSharp needs a native library. For Windows and macOS it comes with SkiaSharp itself. On
Linux, add one of these packages to your application:

- `SkiaSharp.NativeAssets.Linux.NoDependencies`: needs no other library from the system. This
  is the simple choice, and what the examples in the repository use.
- `SkiaSharp.NativeAssets.Linux`: finds fonts through the library `libfontconfig`, which has to
  be installed on the system. Without it SkiaSharp does not start at all.

Both find the fonts that are installed on the system. A system without any font (many
containers) draws no text; install a font package, or load a font file with
`SKTypeface.FromFile`.

## Do you need this package?

Not for loading image files. The SDK reads BMP, PNG and JPEG files itself:

```csharp
var key = KeyBitmap.Create.FromFile("icon.png");
```

You need it when you want to draw at run time, for example text. On Windows only,
[OpenMacroBoard.SDK.KeyBitmap.GDI](../OpenMacroBoard.SDK.KeyBitmap.GDI) does the same with
`System.Drawing`.
