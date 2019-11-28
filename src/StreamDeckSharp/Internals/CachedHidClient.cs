﻿using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            imageQueue = new ConcurrentBufferedQueue<int, byte[]>(RelativeTimeSource.Default, hardwareInformation.KeyCooldown);
            writerTask = StartBitmapWriterTask();
        }

        public override void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            VerifyNotDisposed();
            keyId = hwInfo.ExtKeyIdToHardwareKeyId(keyId);

            var payload = cacheKeyBitmaps.GetValue(bitmapData, hwInfo.GeneratePayload);
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
            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    KeyValuePair<int, byte[]> res;

                    try
                    {
                        res = imageQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }

                    foreach (var report in OutputReportSplitter.Split(res.Value, buffer, hwInfo.ReportSize, hwInfo.HeaderSize, res.Key, hwInfo.PrepareDataForTransmittion))
                    {
                        deckHid.WriteReport(report);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
