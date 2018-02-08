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

        public readonly KeyBitmapHolder[] KeyInfo;
        private readonly WaitHandle[] allWaitHandles;
        private readonly WaitHandle cancelWaitHandle;

        private int keyTestOffset = -1;
        private bool disposed = false;

        private const int exitCode = HidClient.numOfKeys;

        /// <summary>
        /// Value choosen after many experiments ;-)
        /// </summary>
        /// <remarks>
        /// If we write as fast as possible to the StreamDeck, glitches start to appear.
        /// The StreamDeck couldn't keep up and (I guess) startet to write over
        /// buffers that are still used to draw other images.
        /// 
        /// Rewrote the RepaintQueue to be lock-free and to make sure every key has enough
        /// time to "cool down" ;-)
        /// 
        /// I did some experiments with different values (rendered a video "fullscreen")
        /// 50ms ( 20 FPS) caused glitches after about 10 seconds
        /// 65ms (~15 FPS) worked for about 30 seconds
        /// 70ms (~14 FPS) never saw a glitch
        /// 
        /// so I decided to use 75ms (~ 13 FPS) just to be on the safe side
        /// and 13 FPS should be enough for most applications
        /// (the video still looks fine)
        /// </remarks>
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

        public (bool success, int keyId, byte[] data) Dequeue()
        {
            var keyId = WaitForKeyOrExit();
            if (keyId == exitCode) return (false, -1, null);
            return (true, keyId, KeyInfo[keyId].bitmapData);
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
