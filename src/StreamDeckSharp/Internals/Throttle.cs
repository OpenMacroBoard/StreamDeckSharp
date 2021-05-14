using System;
using System.Diagnostics;
using System.Threading;

namespace StreamDeckSharp.Internals
{
    internal class Throttle
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private long sumBytesInWindow = 0;
        private int sleepCount = 0;

        public double BytesPerSecondLimit { get; set; } = double.PositiveInfinity;
        public int ByteCountBeforeThrottle { get; set; } = 16_000;

        public void MeasureAndBlock(int bytes)
        {
            sumBytesInWindow += bytes;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var estimatedSeconds = sumBytesInWindow / BytesPerSecondLimit;

            if (sumBytesInWindow > ByteCountBeforeThrottle && elapsedSeconds < estimatedSeconds)
            {
                var delta = Math.Max(1, (int)((estimatedSeconds - elapsedSeconds) * 1000));
                Thread.Sleep(delta);
                sleepCount++;
            }

            if (elapsedSeconds >= 1)
            {
                if (sleepCount > 1)
                {
                    Debug.WriteLine($"[Throttle] {sumBytesInWindow / elapsedSeconds}");
                }

                stopwatch.Restart();
                sumBytesInWindow = 0;
                sleepCount = 0;
            }
        }
    }
}
