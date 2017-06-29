using HidLibrary;
using StreamDeckSharp.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//Special thanks to Lange (Alex Van Camp) - https://github.com/Lange/node-elgato-stream-deck
//The node-js implementation was the basis of this .NET C# implementation

namespace StreamDeckSharp
{
    /// <summary>
    /// A (very simple) .NET Wrapper for the StreamDeck HID
    /// </summary>
    internal sealed class StreamDeckHID : IStreamDeck
    {
        //At the moment Stream Deck has 15 keys. In the future there may be
        //versions with more or less keys. You should use this property
        //instead of a fixed number or custom const value.
        public int NumberOfKeys { get { return numOfKeys; } }
        public event EventHandler<StreamDeckKeyEventArgs> KeyPressed;

        private HidDevice device;
        private byte[] keyStates = new byte[numOfKeys];
        private volatile bool disposed = false;

        internal const int numOfKeys = 15;
        internal const int iconSize = 72;
        internal const int rawBitmapDataLength = iconSize * iconSize * 3;
        internal const int pagePacketSize = 8191;
        internal const int numFirstPagePixels = 2583;
        internal const int numSecondPagePixels = 2601;

        internal const int vendorId = 0x0fd9;    //Elgato Systems GmbH
        internal const int productId = 0x0060;   //Stream Deck

        private readonly Task[] backgroundTasks;
        private readonly CancellationTokenSource threadCancelSource = new CancellationTokenSource();
        private readonly KeyRepaintQueue qqq = new KeyRepaintQueue();
        private readonly object disposeLock = new object();

        private static readonly byte[] headerTemplatePage1 = new byte[] {
            0x02, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x42, 0x4d, 0xf6, 0x3c, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x48, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xc0, 0x3c, 0x00, 0x00, 0xc4, 0x0e,
            0x00, 0x00, 0xc4, 0x0e, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private static readonly byte[] headerTemplatePage2 = new byte[] {
            0x02, 0x01, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public void SetBrightness(byte percent)
        {
            VerifyNotDisposed();
            if (percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            var buffer = new byte[] { 0x05, 0x55, 0xaa, 0xd1, 0x01, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            buffer[5] = percent;
            device.WriteFeatureData(buffer);
        }

        public void SetKeyBitmap(int keyId, byte[] bitmapData)
        {
            VerifyNotDisposed();
            if (bitmapData != null && bitmapData.Length != (iconSize * iconSize * 3)) throw new NotSupportedException();
            qqq.Enqueue(keyId, bitmapData);
        }

        public void Dispose()
        {
            lock (disposeLock)
            {
                if (disposed) return;
                disposed = true;
            }

            if (device == null) return;

            threadCancelSource.Cancel();
            Task.WaitAll(backgroundTasks);

            device.CloseDevice();
            device.Dispose();
            device = null;
        }

        internal StreamDeckHID(HidDevice device)
        {
            if (device == null) throw new ArgumentNullException();
            if (device.IsOpen) throw new NotSupportedException();
            device.MonitorDeviceEvents = true;
            device.ReadReport(ReadCallback);
            device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
            if (!device.IsOpen) throw new Exception("Device could not be opened");
            this.device = device;

            for (int i = 0; i < numOfKeys; i++)
            {
                keyLocks[i] = new object();
            }

            var numberOfTasks = NumberOfKeys;
            backgroundTasks = new Task[numberOfTasks];
            for (int i = 0; i < numberOfTasks; i++)
            {
                backgroundTasks[i] = Task.Factory.StartNew(() =>
                {
                    var cancelToken = threadCancelSource.Token;

                    while (true)
                    {
                        var res = qqq.Dequeue(out Tuple<int, byte[]> nextBm, cancelToken);
                        if (!res) break;

                        var id = nextBm.Item1;
                        lock (keyLocks[id])
                        {
                            device.Write(GeneratePage1(id, nextBm.Item2));
                            device.Write(GeneratePage2(id, nextBm.Item2));
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        private void VerifyNotDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(StreamDeckHID));
        }

        private void ReadCallback(HidReport report)
        {
            var _d = device;
            if (_d == null || disposed) return;
            ProcessNewStates(report.Data);
            _d.ReadReport(ReadCallback);
        }

        private readonly object[] keyLocks = new object[numOfKeys];


        private void ProcessNewStates(byte[] newStates)
        {
            for (int i = 0; i < numOfKeys; i++)
            {
                if (keyStates[i] != newStates[i])
                {
                    KeyPressed?.Invoke(this, new StreamDeckKeyEventArgs(i, newStates[i] != 0));
                    keyStates[i] = newStates[i];
                }
            }
        }

        private static byte[] GeneratePage1(int keyId, byte[] imgData)
        {
            var p1 = new byte[pagePacketSize];
            Array.Copy(headerTemplatePage1, p1, headerTemplatePage1.Length);

            if (imgData != null)
                Array.Copy(imgData, 0, p1, headerTemplatePage1.Length, numFirstPagePixels * 3);

            p1[5] = (byte)(keyId + 1);
            return p1;
        }

        private static byte[] GeneratePage2(int keyId, byte[] imgData)
        {
            var p2 = new byte[pagePacketSize];
            Array.Copy(headerTemplatePage2, p2, headerTemplatePage2.Length);

            if (imgData != null)
                Array.Copy(imgData, numFirstPagePixels * 3, p2, headerTemplatePage2.Length, numSecondPagePixels * 3);

            p2[5] = (byte)(keyId + 1);
            return p2;
        }
    }

}
