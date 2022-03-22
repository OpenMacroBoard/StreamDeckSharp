using StreamDeckSharp.Internals;
using System.Collections.Generic;

namespace StreamDeckSharp.Tests
{
    internal static class HardwareInfoResolver
    {
        internal static IEnumerable<IHardwareInternalInfos> GetAllHardwareInfos()
        {
            yield return Hardware.Internal_StreamDeck;
            yield return Hardware.Internal_StreamDeckMini;
            yield return Hardware.Internal_StreamDeckMK2;
            yield return Hardware.Internal_StreamDeckRev2;
            yield return Hardware.Internal_StreamDeckXL;
        }
    }
}
