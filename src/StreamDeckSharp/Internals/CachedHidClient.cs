using OpenMacroBoard.SDK;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckSharp.Internals
{
    internal class CachedHidClient : BasicHidClient
    {
        private readonly Task writerTask;
        private readonly ConcurrentBufferedQueue<int, byte[]> imageQueue;
        private readonly ConditionalWeakTable<KeyBitmap, byte[]> cacheKeyBitmaps = new();

        public CachedHidClient(
            IStreamDeckHid deckHid,
            IKeyLayout keys,
            IStreamDeckHidComDriver hidComDriver
        )
            : base(deckHid, keys, hidComDriver)
        {
            imageQueue = new ConcurrentBufferedQueue<int, byte[]>();
            writerTask = StartBitmapWriterTask();
        }

        public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            ThrowIfAlreadyDisposed();
            keyId = HidComDriver.ExtKeyIdToHardwareKeyId(keyId);

            var payload = cacheKeyBitmaps.GetValue(bitmapData, HidComDriver.GeneratePayload);
            imageQueue.Add(keyId, payload);
        }

        protected override void Shutdown()
        {
            imageQueue.CompleteAdding();
            writerTask.Wait();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            imageQueue.Dispose();
        }

        private Task StartBitmapWriterTask()
        {
            void BackgroundAction()
            {
                while (true)
                {
                    var (success, keyId, payload) = imageQueue.Take();

                    if (!success)
                    {
                        // image queue completed
                        break;
                    }

                    var reports = OutputReportSplitter.Split(
                        payload,
                        Buffer,
                        HidComDriver.ReportSize,
                        HidComDriver.HeaderSize,
                        keyId,
                        HidComDriver.PrepareDataForTransmission
                    );

                    foreach (var report in reports)
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
