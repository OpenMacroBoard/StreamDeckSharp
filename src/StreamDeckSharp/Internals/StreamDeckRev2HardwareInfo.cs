using OpenMacroBoard.SDK;
using static StreamDeckSharp.UsbConstants;

namespace StreamDeckSharp.Internals
{
    internal sealed class StreamDeckRev2HardwareInfo
        : StreamDeckJpgHardwareBase
    {
        public StreamDeckRev2HardwareInfo()
            : base(new GridKeyLayout(5, 3, 72, 32))
        {
        }

        public override string DeviceName => "Stream Deck Rev2";
        public override int UsbProductId => ProductIds.StreamDeckRev2;

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Limit of 3'200'000 bytes/s (~3.0 MiB/s) just to be safe,
        /// because I don't own a StreamDeck Rev2 to test it.
        /// </para>
        /// <para>
        /// See <see cref="StreamDeckHidWrapper"/> for details.
        /// </para>
        /// </remarks>
        public override double BytesPerSecondLimit => 3_200_000;
    }
}
