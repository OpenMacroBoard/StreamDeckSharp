using System;

namespace StreamDeckSharp.Internals
{
    internal class ReportReceivedEventArgs : EventArgs
    {
        public ReportReceivedEventArgs(byte[] reportData)
        {
            ReportData = reportData;
        }

        public byte[] ReportData { get; }
    }
}
