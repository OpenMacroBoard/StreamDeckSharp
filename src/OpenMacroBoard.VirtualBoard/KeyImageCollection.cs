using System.Windows.Media.Imaging;

namespace OpenMacroBoard.VirtualBoard
{
    internal class KeyImageCollection
    {
        private readonly BitmapSource[] keyImages;

        public KeyImageCollection(int cnt)
        {
            keyImages = new BitmapSource[cnt];
        }

        public BitmapSource this[int index]
        {
            get => keyImages[index];
            set => keyImages[index] = value;
        }

        public int Count
            => keyImages.Length;
    }
}
