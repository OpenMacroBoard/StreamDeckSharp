using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace StreamDeckSharp.Internals
{
    internal static class RelativeTimeSource
    {
        private class StopwatchTimesource : ITimeService
        {
            private readonly Stopwatch sw;

            public StopwatchTimesource()
            {
                sw = Stopwatch.StartNew();
            }

            public long GetRelativeTimestamp()
                => sw.ElapsedMilliseconds;
        }

        public static ITimeService Default { get; }

        static RelativeTimeSource()
        {
            Default = new StopwatchTimesource();
        }
    }
}
