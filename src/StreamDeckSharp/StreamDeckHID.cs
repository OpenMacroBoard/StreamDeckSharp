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
        public int NumberOfKeys => numOfKeys;

        public event EventHandler<StreamDeckKeyEventArgs> KeyPressed;
        public event EventHandler<StreamDeckConnectionEventArgs> ConnectionStateChanged;

        private HidDevice device;
        private byte[] keyStates = new byte[numOfKeys];
        private volatile bool disposed = false;

        internal const int numOfKeys = 15;
        internal const int iconSize = 72;
        internal const int rawBitmapDataLength = iconSize * iconSize * 3;

        private readonly Task[] backgroundTasks;
        private readonly CancellationTokenSource threadCancelSource = new CancellationTokenSource();
        private readonly KeyRepaintQueue qqq = new KeyRepaintQueue();
        private readonly object disposeLock = new object();

        public int IconSize => iconSize;
        public bool IsConnected => device.IsConnected;

        public void SetKeyBitmap(int keyId, StreamDeckKeyBitmap bitmap)
        {
            VerifyNotDisposed();
            qqq.Enqueue(keyId, bitmap?.rawBitmapData);
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

            ShowLogoWithoutDisposeVerification();

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

            this.device.Inserted += Device_Inserted;
            this.device.Removed += Device_Removed;

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
                            var page1 = StreamDeckCom.GeneratePage1(id, nextBm.Item2);
                            var page2 = StreamDeckCom.GeneratePage2(id, nextBm.Item2);

                            device.Write(page1, 250);
                            device.Write(page2, 250);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        private void Device_Removed()
        {
            var arg = new StreamDeckConnectionEventArgs(false);
            ConnectionStateChanged?.Invoke(this, arg);
        }

        private void Device_Inserted()
        {
            var arg = new StreamDeckConnectionEventArgs(true);
            ConnectionStateChanged?.Invoke(this, arg);
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

        public void SetBrightness(byte percent)
        {
            VerifyNotDisposed();
            device.WriteFeatureData(StreamDeckCom.GetBrightnessMsg(percent));
        }

        public void ShowLogo()
        {
            VerifyNotDisposed();
            ShowLogoWithoutDisposeVerification();
        }

        private void ShowLogoWithoutDisposeVerification()
        {
            device.WriteFeatureData(StreamDeckCom.ShowLogoMsg);
        }
    }

}
