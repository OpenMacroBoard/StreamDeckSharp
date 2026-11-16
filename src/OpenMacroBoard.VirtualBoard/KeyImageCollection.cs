using System.Windows.Media.Imaging;

namespace OpenMacroBoard.VirtualBoard;

internal sealed class KeyImageCollection(int cnt)
{
    private readonly BitmapSource[] keyImages = new BitmapSource[cnt];

    public int Count
        => keyImages.Length;

    public BitmapSource this[int index]
    {
        get => keyImages[index];
        set => keyImages[index] = value;
    }
}
