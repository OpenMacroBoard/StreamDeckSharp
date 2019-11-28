using OpenMacroBoard.SDK;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckXlHardwareInfo
        : StreamDeckJpgHardwareBase
    {
        public override string DeviceName => "Stream Deck XL";
        public override int UsbProductId => ProductIds.StreamDeckXL;

        public StreamDeckXlHardwareInfo()
            : base(new GridKeyPositionCollection(8, 4, 96, 25))
        {
        }
    }
}
