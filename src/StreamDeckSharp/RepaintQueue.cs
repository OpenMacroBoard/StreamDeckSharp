using System;
using System.Collections.Generic;
using System.Threading;

namespace StreamDeckSharp
{
    internal sealed class KeyRepaintQueue
    {
        private class KeyBitmapHolder
        {
            public byte[] bitmapData;
        }

        private readonly Queue<int> keyQueue = new Queue<int>();
        private readonly KeyBitmapHolder[] keyIndex = new KeyBitmapHolder[StreamDeckHID.numOfKeys];
        private readonly object listLock = new object();
        private readonly SemaphoreSlim waiter = new SemaphoreSlim(0);

        public void Enqueue(int keyId, byte[] data)
        {
            lock (listLock)
            {
                if (keyIndex[keyId] == null)
                {
                    //enque
                    keyQueue.Enqueue(keyId);
                    keyIndex[keyId] = new KeyBitmapHolder();
                    waiter.Release();
                }

                //update
                keyIndex[keyId].bitmapData = data;
            }
        }

        public bool Dequeue(out Tuple<int, byte[]> info, CancellationToken token)
        {
            byte[] outdata;
            int keyId;

            try { waiter.Wait(token); }
            catch (OperationCanceledException) { }

            lock (listLock)
            {
                if (keyQueue.Count < 1)
                {
                    info = null;
                    return false;
                }

                keyId = keyQueue.Dequeue();
                outdata = keyIndex[keyId].bitmapData;
                keyIndex[keyId] = null;
            }

            info = new Tuple<int, byte[]>(keyId, outdata);
            return true;
        }

        public KeyRepaintQueue()
        {

        }
    }

}
