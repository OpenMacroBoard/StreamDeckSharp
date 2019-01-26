using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;
using OpenMacroBoard.SDK;

namespace StreamDeckSharp.Internals
{
    internal class CachedHidClient : BasicHidClient
    {
        private readonly CancellationTokenSource threadCancelSource = new CancellationTokenSource();
        private readonly ConcurrentBufferedQueue<int, byte[]> imageQueue;

        private readonly Task writerTask;
        private readonly Task keyPollingTask;

        private readonly ConditionalWeakTable<KeyBitmap, byte[]> cacheKeyBitmaps = new ConditionalWeakTable<KeyBitmap, byte[]>();

        private CachedHidClient(IStreamDeckHid deckHid, IHardwareInternalInfos hardwareInformation)
            : base(deckHid, hardwareInformation)
        {
            imageQueue = new ConcurrentBufferedQueue<int, byte[]>(RelativeTimeSource.Default, 75);

            writerTask = StartBitmapWriterTask();
            keyPollingTask = StartKeyPollingTask();
        }

        private Task StartBitmapWriterTask()
        {
            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var res = imageQueue.Take();
                        reportGenerator.Initialize(res.Value, res.Key);
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }

                    while (reportGenerator.HasNextReport)
                        deckHid.WriteReport(reportGenerator.GetNextReport());
                }
            }, TaskCreationOptions.LongRunning);
        }

        private Task StartKeyPollingTask()
        {
            return Task.Factory.StartNew(() =>
            {
                var cancelToken = threadCancelSource.Token;
                while (!cancelToken.IsCancellationRequested)
                {
                    ProcessKeys();
                }
            }, TaskCreationOptions.LongRunning);
        }

        public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            VerifyNotDisposed();
            keyId = hardwareInformation.ExtKeyIdToHardwareKeyId(keyId);

            var payload = cacheKeyBitmaps.GetValue(bitmapData, hardwareInformation.GeneratePayload);
            imageQueue.Add(keyId, payload);
        }

        public static IStreamDeckBoard FromHid(HidDevice device)
        {
            var hidWrapper = new StreamDeckHidWrapper(device);
            return new CachedHidClient(hidWrapper, device.GetHardwareInformation());
        }

        protected override void Shutdown()
        {
            imageQueue.CompleteAdding();
            threadCancelSource.Cancel();
            Task.WaitAll(writerTask);
        }

        protected override void Dispose(bool managed)
        {
            Task.WaitAll(keyPollingTask);
            imageQueue.Dispose();
            threadCancelSource.Dispose();
        }
    }
}
