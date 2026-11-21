using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SkiaSharp;
using System.Globalization;
using System.Reflection;

namespace OpenMacroBoard.Examples.Minesweeper;

internal class ClassicMinesweeperIconSet : IMinesweeperIconSet
{
    private readonly KeyBitmap[] numberBitmaps = new KeyBitmap[9];
    private readonly SKFont font = ExampleFonts.Create(55);

    public ClassicMinesweeperIconSet()
    {
        InitializeBitmaps();
    }

    public KeyBitmap ExplodedMine { get; } = LoadExampleImageFromResources("mine.png");
    public KeyBitmap DefusedMine { get; } = LoadExampleImageFromResources("check.png");
    public KeyBitmap FlagWithoutMine { get; } = LoadExampleImageFromResources("cross.png");
    public KeyBitmap Flag { get; } = LoadExampleImageFromResources("flag.png");
    public KeyBitmap HiddenCell { get; } = LoadExampleImageFromResources("unknown.png");

    public KeyBitmap this[int number] => numberBitmaps[number];

    private static KeyBitmap LoadExampleImageFromResources(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = $"OpenMacroBoard.Examples.Minesweeper.icons.{name}";

        using var resStream = asm.GetManifestResourceStream(resourceName);

        // The SDK reads PNG files itself; transparent parts become black.
        return KeyBitmap.Create.FromStream(resStream);
    }

    private void InitializeBitmaps()
    {
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        for (var i = 0; i < numberBitmaps.Length; i++)
        {
            var number = i.ToString(CultureInfo.InvariantCulture);

            // SkiaSharp places text by its baseline
            numberBitmaps[i] = KeyBitmap.Create.FromSkiaCanvas(
                96,
                96,
                canvas => canvas.DrawText(number, 30, 68, SKTextAlign.Left, font, paint)
            );
        }

        numberBitmaps[0] = KeyBitmap.Black;
    }
}
