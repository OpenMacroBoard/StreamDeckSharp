using System;

namespace StreamDeckSharp.Internals
{
    internal class OutputReportGenerator
    {
        private const int pageHeaderLength = 16;

        private readonly byte[] buffer;
        private readonly int payloadLimit;
        private readonly int startPageNum;
        private byte[] payload;
        private int bytesSent;
        private int currentPage;

        public bool HasNextReport { get; private set; }

        public OutputReportGenerator(int reportLength, int payloadLimit, int startPageNum)
        {
            buffer = new byte[reportLength];
            buffer[0] = 2;
            buffer[1] = 1;
            this.payloadLimit = payloadLimit;
            this.startPageNum = startPageNum;
        }

        public void Initialize(byte[] payload, int keyId)
        {
            this.payload = payload;
            buffer[5] = (byte)(keyId + 1);
            bytesSent = 0;
            currentPage = 0;
            HasNextReport = true;
        }

        public byte[] GetNextReport()
        {
            var remainingBytes = payload.Length - bytesSent;

            var transferLength = 0;
            var isLast = false;

            if (remainingBytes <= payloadLimit)
            {
                transferLength = remainingBytes;
                isLast = true;
            }
            else
            {
                transferLength = payloadLimit;
            }

            buffer[2] = (byte)(currentPage + startPageNum);
            buffer[4] = (byte)(isLast ? 1 : 0);

            //ToDo: set islast to buffer
            Array.Copy(payload, bytesSent, buffer, pageHeaderLength, transferLength);

            var bufferUsed = pageHeaderLength + transferLength;
            var remainingBufferLen = buffer.Length - bufferUsed;

            Array.Clear(buffer, bufferUsed, remainingBufferLen);

            if (isLast)
                HasNextReport = false;

            bytesSent += transferLength;
            currentPage++;
            return buffer;
        }
    }
}
