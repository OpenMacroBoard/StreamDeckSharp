using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace StreamDeckSharp
{
    internal sealed class KeyRepaintQueue : IDisposable
    {
        public class KeyBitmapHolder
        {
            public volatile byte[] bitmapData;
            public readonly AutoResetEvent dirtyEvent = new AutoResetEvent(false);
            public readonly Stopwatch lastDraw = Stopwatch.StartNew();
        }

        public struct DequeueElement
        {
            public bool success;
            public int keyId;
            public byte[] data;

            public DequeueElement(bool success, int keyId, byte[] data)
            {
                this.success = success;
                this.keyId = keyId;
                this.data = data;
            }
        }

        public readonly KeyBitmapHolder[] KeyInfo;
        private readonly WaitHandle[] allWaitHandles;
        private readonly WaitHandle cancelWaitHandle;

        private int keyTestOffset = -1;
        private bool disposed = false;

        private const int exitCode = HidClient.numOfKeys;

        /// <summary>
        /// Value choosen after many experiments ;-)
        /// See github wiki...
        /// </summary>
        private const int keyCooldownTime = 75;//ms

        public KeyRepaintQueue(CancellationToken token)
        {
            KeyInfo = new KeyBitmapHolder[HidClient.numOfKeys];
            allWaitHandles = new WaitHandle[HidClient.numOfKeys + 1];

            for (int i = 0; i < 15; i++)
            {
                var holder = new KeyBitmapHolder();
                KeyInfo[i] = holder;
                allWaitHandles[i] = holder.dirtyEvent;
            }

            allWaitHandles[exitCode] = token.WaitHandle;
            cancelWaitHandle = token.WaitHandle;
        }

        public void Enqueue(int keyId, byte[] data)
        {
            if (disposed) return;

            KeyInfo[keyId].bitmapData = data;
            KeyInfo[keyId].dirtyEvent.Set();
        }

        public DequeueElement Dequeue()
        {
            var keyId = WaitForKeyOrExit();
            if (keyId == exitCode) return new DequeueElement(false, -1, null);
            return new DequeueElement(true, keyId, KeyInfo[keyId].bitmapData);
        }

        private int WaitForKeyOrExit()
        {
            if (disposed) return exitCode;

            //rotate what key is tested first
            //to make sure that every key has the same chance of beeing updated
            keyTestOffset++;
            if (keyTestOffset >= HidClient.numOfKeys)
                keyTestOffset = 0;

            while (true)
            {
                //Test if cancelation was requested
                if (cancelWaitHandle.WaitOne(0))
                    return exitCode;

                var couldBePressedAnyMomentBuilder = new List<WaitHandle>();
                var waitAtMost = keyCooldownTime + 1;
                couldBePressedAnyMomentBuilder.Add(cancelWaitHandle);

                for (int i = 0; i < HidClient.numOfKeys; i++)
                {
                    //calculate effective id (offset rotation)
                    var id = (i + keyTestOffset) % HidClient.numOfKeys;

                    var t = (int)KeyInfo[id].lastDraw.ElapsedMilliseconds;
                    var isCooledDown = t > keyCooldownTime;

                    var keyHandle = KeyInfo[id].dirtyEvent;
                    var keyWasPressed = keyHandle.WaitOne(0);

                    if (keyWasPressed && isCooledDown)
                    {
                        KeyInfo[id].lastDraw.Restart();
                        return id;
                    }
                    else
                    {
                        //don't want to consume it right now
                        if (keyWasPressed) keyHandle.Set();

                        if (isCooledDown)   //... but was not pressed
                        {
                            couldBePressedAnyMomentBuilder.Add(keyHandle);
                        }
                        else if (keyWasPressed) //... but not longer then frame
                        {
                            var diff = keyCooldownTime - t;
                            if (diff < waitAtMost)
                                waitAtMost = diff;
                        }
                    }
                }

                int candidateCount = couldBePressedAnyMomentBuilder.Count;
                if (candidateCount > 0)
                {
                    var couldBePressedAnyMoment = couldBePressedAnyMomentBuilder.ToArray();

                    //If every key (+the cancel token) is a candidate, disable timeout
                    if (candidateCount == (HidClient.numOfKeys + 1))
                        waitAtMost = Timeout.Infinite;

                    var candidateIndex = WaitHandle.WaitAny(couldBePressedAnyMoment, waitAtMost);

                    if (candidateIndex == 0)//canceltoken signaled
                        return exitCode;

                    //que the signal back in (will be consumed inside the for loop)
                    if (candidateIndex == WaitHandle.WaitTimeout)
                        continue;

                    var keyDirty = (AutoResetEvent)couldBePressedAnyMoment[candidateIndex];
                    keyDirty.Set();
                }
                else
                {
                    Thread.Sleep(waitAtMost);
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            for (int i = 0; i < HidClient.numOfKeys; i++)
                KeyInfo[i].dirtyEvent.Dispose();
        }
    }
}
