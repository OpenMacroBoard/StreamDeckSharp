using System.Diagnostics;

namespace StreamDeckSharp.Internals
{
    internal static class RelativeTimeSource
    {
        static RelativeTimeSource()
        {
            Default = new StopwatchTimesource();
        }

        public static ITimeService Default { get; }

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
    }
}
