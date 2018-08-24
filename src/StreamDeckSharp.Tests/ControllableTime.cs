using StreamDeckSharp.Internals;

namespace StreamDeckSharp.Tests
{
    public class ControllableTime : ITimeService
    {
        public long Timestamp { get; set; }

        public ControllableTime()
        {

        }

        public void Add(long offset)
            => Timestamp += offset;

        public long GetRelativeTimestamp()
            => Timestamp;
    }
}
