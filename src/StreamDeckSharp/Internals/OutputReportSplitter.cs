using System;
using System.Collections.Generic;

namespace StreamDeckSharp.Internals
{
    internal static class OutputReportSplitter
    {
        public delegate void PrepareDataForTransmittion(byte[] data, int pageNumber, int payloadLength, int keyId, bool isLast);

        public static IEnumerable<byte[]> Split(
            byte[] data,
            byte[] buffer,
            int bufferLength,
            int headerSize,
            int keyId,
            PrepareDataForTransmittion prepareData)
        {
            var maxPayloadLength = bufferLength - headerSize;

            var remainingBytes = data.Length;
            var bytesSent = 0;
            var splitNumber = 0;

            while (remainingBytes > 0)
            {
                var isLast = remainingBytes <= maxPayloadLength;
                var bytesToSend = Math.Min(remainingBytes, maxPayloadLength);

                Array.Copy(data, bytesSent, buffer, headerSize, bytesToSend);
                prepareData(buffer, splitNumber, bytesToSend, keyId, isLast);
                yield return buffer;

                bytesSent += bytesToSend;
                remainingBytes -= bytesToSend;
                splitNumber++;
            }
        }
    }
}
