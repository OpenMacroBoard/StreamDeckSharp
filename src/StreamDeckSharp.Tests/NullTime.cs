using StreamDeckSharp.Internals;

namespace StreamDeckSharp.Tests
{
    internal class NullTime : ITimeService
    {
        public static ITimeService Source { get; } = new NullTime();

        private NullTime() { }
        public long GetRelativeTimestamp() => 0;
    }
}
