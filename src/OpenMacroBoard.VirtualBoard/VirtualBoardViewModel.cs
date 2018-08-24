using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenMacroBoard.SDK;

namespace OpenMacroBoard.VirtualBoard
{
    /// <summary>
    /// A view model for a virtual macro board
    /// </summary>
    internal class VirtualBoardViewModel : INotifyPropertyChanged, IMacroBoard
    {
        /// <summary>
        /// Is fired of one of the properties with binding support is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Is fired if the state of a key changes.
        /// </summary>
        public event EventHandler<KeyEventArgs> KeyStateChanged;

        /// <summary>
        /// Is fired if the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        private readonly Dispatcher dispatcher;

        private bool isConnected = true;

        /// <summary>
        /// Constructs a new view model for a virtual macro board.
        /// </summary>
        /// <param name="keyLayout"></param>
        public VirtualBoardViewModel(IKeyPositionCollection keyLayout)
        {
            Keys = keyLayout ?? throw new ArgumentNullException(nameof(keyLayout));
            KeyImages = new KeyImageCollection(Keys.Count);

            dispatcher = Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// The key layout for this macro board
        /// </summary>
        public IKeyPositionCollection Keys { get; }

        /// <summary>
        /// The images currently set to this macro board
        /// </summary>
        public KeyImageCollection KeyImages { get; }

        /// <summary>
        /// Gets a value that indicated whether the board is connected or not.
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (value == isConnected)
                    return;

                isConnected = value;
                ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs(value));
            }
        }

        /// <summary>
        /// Sets the current brightness
        /// </summary>
        /// <param name="percent"></param>
        public void SetBrightness(byte percent)
        {

        }

        /// <summary>
        /// Sets a new <see cref="KeyBitmap"/> to a specific key.
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="bitmapData"></param>
        public void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            var srcData = (IKeyBitmapDataAccess)bitmapData;
            var data = srcData.CopyData();

            var wb = new WriteableBitmap(bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null);
            if (data != null)
                wb.WritePixels(new Int32Rect(0, 0, bitmapData.Width, bitmapData.Height), data, srcData.Stride, 0);
            wb.Freeze();

            KeyImages[keyId] = wb;
            RaiseKeyImagesChanges();
        }

        /// <summary>
        /// Shows the standby logo
        /// </summary>
        public void ShowLogo()
        {

        }

        /// <summary>
        /// Disposes the <see cref="VirtualBoardViewModel"/>.
        /// </summary>
        public void Dispose()
        {

        }

        internal void SendKeyState(int keyId, bool down)
        {
            KeyStateChanged?.Invoke(this, new KeyEventArgs(keyId, down));
        }

        private void RaiseKeyImagesChanges()
        {
            dispatcher.Invoke(new Action(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeyImages)));
            }));
        }
    }
}
