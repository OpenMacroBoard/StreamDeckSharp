using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Reflection;

namespace OpenMacroBoard.Examples.Minesweeper;

internal class ClassicMinesweeperIconSet : IMinesweeperIconSet
{
    private readonly KeyBitmap[] numberBitmaps = new KeyBitmap[9];
    private readonly Font font = SystemFonts.CreateFont("Arial", 55);

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
        using var bmp = Image.Load(resStream);

        return KeyBitmap.Create.FromImageSharpImage(bmp);
    }

    private void InitializeBitmaps()
    {
        for (var i = 0; i < numberBitmaps.Length; i++)
        {
            using var keyImage = new Image<Bgr24>(96, 96);

            keyImage.Mutate(x => x.DrawText(
                i.ToString(CultureInfo.InvariantCulture),
                font,
                Color.White,
                new PointF(30, 18)
            ));

            numberBitmaps[i] = KeyBitmap.Create.FromImageSharpImage(keyImage);
        }

        numberBitmaps[0] = KeyBitmap.Black;
    }
}
