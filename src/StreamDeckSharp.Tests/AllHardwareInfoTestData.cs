using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp.Tests
{
    public class AllHardwareInfoTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            return Hardware
                .GetInternalStreamDeckHardwareInfos()
                .Select(h => new object[] { h })
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
