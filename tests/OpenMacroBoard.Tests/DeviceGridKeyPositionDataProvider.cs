using OpenMacroBoard.SDK;
using System.Collections;
using System.Collections.Generic;

namespace OpenMacroBoard.Tests;

public class DeviceGridKeyPositionDataProvider : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var (name, keys) in GetData())
        {
            yield return new object[] { name, keys };
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<(string DeviceName, GridKeyLayout Keys)> GetData()
    {
        yield return ("5x3", Medium);
        yield return ("8x4", Large);
        yield return ("3x2", Small);
    }

    public static GridKeyLayout Medium { get; } = new(5, 3, 72, 30);
    public static GridKeyLayout Large { get; } = new(8, 4, 96, 38);
    public static GridKeyLayout Small { get; } = new(3, 2, 80, 32);
}
