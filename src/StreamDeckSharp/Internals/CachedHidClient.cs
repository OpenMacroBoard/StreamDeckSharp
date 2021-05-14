using OpenMacroBoard.SDK;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckSharp.Internals
{
    internal class CachedHidClient : BasicHidClient
    {
        private readonly Task writerTask;
        private readonly ConcurrentBufferedQueue<int, byte[]> imageQueue;
        private readonly ConditionalWeakTable<KeyBitmap, byte[]> cacheKeyBitmaps = new ConditionalWeakTable<KeyBitmap, byte[]>();

        public CachedHidClient(IStreamDeckHid deckHid, IHardwareInternalInfos hardwareInformation)
            : base(deckHid, hardwareInformation)
        {
            imageQueue = new ConcurrentBufferedQueue<int, byte[]>();
            writerTask = StartBitmapWriterTask();
        }

        public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            VerifyNotDisposed();
            keyId = HardwareInfo.ExtKeyIdToHardwareKeyId(keyId);

            var payload = cacheKeyBitmaps.GetValue(bitmapData, HardwareInfo.GeneratePayload);
            imageQueue.Add(keyId, payload);
        }

        protected override void Shutdown()
        {
            imageQueue.CompleteAdding();
            Task.WaitAll(writerTask);
        }

        protected override void Dispose(bool managed)
        {
            imageQueue.Dispose();
        }

        private Task StartBitmapWriterTask()
        {
            void BackgroundAction()
            {
                while (true)
                {
                    int keyId;
                    byte[] payload;

                    try
                    {
                        (keyId, payload) = imageQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }

                    foreach (var report in OutputReportSplitter.Split(payload, Buffer, HardwareInfo.ReportSize, HardwareInfo.HeaderSize, keyId, HardwareInfo.PrepareDataForTransmittion))
                    {
                        DeckHid.WriteReport(report);
                    }
                }
            }

            return Task.Factory.StartNew(
                BackgroundAction,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }
    }
}
