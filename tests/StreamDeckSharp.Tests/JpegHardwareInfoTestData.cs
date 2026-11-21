using StreamDeckSharp.Internals.HidComDriver;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp.Tests;

/// <summary>
/// All hardware that expects key images as JPEG.
/// </summary>
public class JpegHardwareInfoTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        return Hardware
            .GetInternalStreamDeckHardwareInfos()
            .Where(h => h.Driver is HidComDriverStreamDeckJpeg)
            .Select(h => new object[] { h })
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
