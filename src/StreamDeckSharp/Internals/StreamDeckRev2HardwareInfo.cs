using OpenMacroBoard.SDK;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckRev2HardwareInfo
        : StreamDeckJpgHardwareBase
    {
        public StreamDeckRev2HardwareInfo()
            : base(new GridKeyPositionCollection(5, 3, 72, 32))
        {
        }

        public override string DeviceName => "Stream Deck Rev2";
        public override int UsbProductId => ProductIds.StreamDeckRev2;
    }
}
