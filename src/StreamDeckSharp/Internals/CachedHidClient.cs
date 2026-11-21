using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals.HidComDriver;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckSharp.Internals;

internal class CachedHidClient : BasicHidClient
{
    private readonly Task writerTask;
    private readonly ConcurrentBufferedQueue<int, byte[]> imageQueue;

    public CachedHidClient(
        IStreamDeckHid deckHid,
        IKeyLayout keys,
        IStreamDeckHidComDriver hidComDriver,
        IJpegEncoder jpegEncoder
    )
        : base(deckHid, keys, hidComDriver, jpegEncoder)
    {
        imageQueue = new();
        writerTask = StartBitmapWriterTask();
    }

    public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
    {
        ThrowIfAlreadyDisposed();
        keyId = HidComDriver.KeyIdMapper.ExtKeyIdToHardwareKeyId(keyId);

        var payload = HidComDriver.GeneratePayload(bitmapData, JpegEncoder);
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
