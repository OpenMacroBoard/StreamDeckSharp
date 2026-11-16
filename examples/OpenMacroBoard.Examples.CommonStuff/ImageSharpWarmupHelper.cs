using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OpenMacroBoard.Examples.CommonStuff;

public static class ImageSharpWarmupHelper
{
    public static void RunWarmup()
    {
        // It looks like image sharp is a bit slow on the first "DrawText" call
        // I haven't looked into it but I guess some internal memory allocation / caching
        // is happening on the first call.

        // https://github.com/SixLabors/ImageSharp.Drawing/issues/187

        // It likely doesn't depend on the font family or size
        // so we just use Arial 12pt for warm-up.

        new Image<Bgr24>(128, 128)
            .Mutate(x => x.DrawText(
                "Random Text",
                SystemFonts.CreateFont("Arial", 12),
                Color.White,
                new PointF(10, 10)
             ));
    }
}
