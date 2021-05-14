using System;
using System.Diagnostics;
using System.Threading;

namespace StreamDeckSharp.Internals
{
    internal class Throttle
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private long sumBytesInWindow = 0;

        public double BytesPerSecondLimit { get; set; } = double.PositiveInfinity;

        public void MeasureAndBlock(int bytes)
        {
            sumBytesInWindow += bytes;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var estimatedSeconds = sumBytesInWindow / BytesPerSecondLimit;

            if (elapsedSeconds < estimatedSeconds)
            {
                var delta = Math.Max(1, (int)((estimatedSeconds - elapsedSeconds) * 1000));
                Thread.Sleep(delta);
            }

            if (elapsedSeconds >= 1)
            {
                stopwatch.Restart();
                sumBytesInWindow = 0;
            }
        }
    }
}
