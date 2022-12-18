using OpenMacroBoard.SDK;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckXlHardwareInfo
        : StreamDeckJpgHardwareBase
    {
        public StreamDeckXlHardwareInfo(int usbProductId, string deviceName)
            : base(new GridKeyLayout(8, 4, 96, 38))
        {
            UsbProductId = usbProductId;
            DeviceName = deviceName;
        }

        public override string DeviceName { get; }
        public override int UsbProductId { get; }
    }
}