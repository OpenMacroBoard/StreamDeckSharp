using OpenMacroBoard.SDK;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckXlHardwareInfo
        : StreamDeckJpgHardwareBase
    {
        public StreamDeckXlHardwareInfo()
            : base(new GridKeyPositionCollection(8, 4, 96, 38))
        {
        }

        public override string DeviceName => "Stream Deck XL";
        public override int UsbProductId => ProductIds.StreamDeckXL;
    }
}
