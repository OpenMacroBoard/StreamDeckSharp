using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;
using System.Drawing;

namespace OpenMacroBoard.Tests;

public class KeyBitmapFactoryVisualTests
{
    public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

    [Fact]
    public async Task GDIArcIsDrawnCorrectly()
    {
        Verifier.Initialize();
        Verifier.UseFileNameAsDirectory();

        var key = KeyBitmap.Create
            .FromGraphics(
                width: 64,
                height: 64,
                g =>
                {
                    g.Clear(Color.Red);
                    g.DrawArc(Pens.Blue, new Rectangle(10, 10, 40, 40), 10, 180);
                }
            );

        await Verifier.VerifyAsync(key);
    }
}
