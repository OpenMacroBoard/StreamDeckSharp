using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    internal interface IStreamDeckHid : IDisposable
    {
        int OutputReportLength { get; }
        bool WriteFeature(byte[] featureData);
        bool WriteReport(byte[] reportData);
        bool ReadFeatureData(byte id, out byte[] data);
        bool IsConnected { get; }

        event EventHandler<ReportReceivedEventArgs> ReportReceived;
        event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
    }
}
