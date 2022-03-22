using OpenMacroBoard.SDK;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckMK2HardwareInfo
        : StreamDeckJpgHardwareBase
    {
        public StreamDeckMK2HardwareInfo()
            : base(new GridKeyLayout(5, 3, 72, 32))
        {
        }

        public override string DeviceName => "Stream Deck MK.2";
        public override int UsbProductId => ProductIds.StreamDeckMK2;
    }
}
