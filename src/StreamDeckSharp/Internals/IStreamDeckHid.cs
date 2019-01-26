using OpenMacroBoard.SDK;
using System;

namespace StreamDeckSharp.Internals
{
    internal interface IStreamDeckHid : IDisposable
    {
        int OutputReportLength { get; }
        bool WriteFeature(byte[] featureData);
        bool WriteReport(byte[] reportData);
        byte[] ReadReport();
        byte[] ReadFeatureData(byte id);
        bool IsConnected { get; }

        event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
    }
}
