using System.Collections.Generic;

namespace System.Linq
{
    internal static class EnumerableCrossReferenceExtension
    {
        public static IEnumerable<(T1 Item1, T2 Item2)> CrossRef<T1, T2>(this IEnumerable<T1> self, IEnumerable<T2> other)
        {
            foreach (var x in self)
            {
                foreach (var y in other)
                {
                    yield return (x, y);
                }
            }
        }
    }
}
