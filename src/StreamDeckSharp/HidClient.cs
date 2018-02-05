using HidLibrary;
using System;
using System.Threading;
using System.Threading.Tasks;

//Special thanks to Lange (Alex Van Camp) - https://github.com/Lange/node-elgato-stream-deck
//The node-js implementation was the basis of this .NET C# implementation

namespace StreamDeckSharp
{
    /// <summary>
    /// A (very simple) .NET Wrapper for the StreamDeck HID
    /// </summary>
    internal sealed class HidClient : IStreamDeck
    {
        //At the moment Stream Deck has 15 keys. In the future there may be
        //versions with more or less keys. You should use this property
        //instead of a fixed number or custom const value.
        public int KeyCount => numOfKeys;

        public event EventHandler<KeyEventArgs> KeyStateChanged;
        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

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

        public void SetKeyBitmap(int keyId, KeyBitmap bitmap)
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
            device.SetAllEvents();
            Task.WaitAll(backgroundTasks);

            ShowLogoWithoutDisposeVerification();

            device.CloseDevice();
            device.Dispose();
            device = null;
        }

        internal HidClient(HidDevice device)
        {
            if (device == null) throw new ArgumentNullException();
            if (device.IsOpen) throw new NotSupportedException();
            device.MonitorDeviceEvents = true;
            device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
            if (!device.IsOpen) throw new Exception("Device could not be opened");
            this.device = device;

            this.device.Inserted += Device_Inserted;
            this.device.Removed += Device_Removed;

            for (int i = 0; i < numOfKeys; i++)
            {
                keyLocks[i] = new object();
            }

            var numberOfWriterThreads = 1;
            var numberOfReadThreads = 1;
            var numberOfThreads = numberOfWriterThreads + numberOfReadThreads;

            backgroundTasks = new Task[numberOfThreads];
            for (int i = 0; i < numberOfWriterThreads; i++)
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
                            var page1 = HidCommunicationHelper.GeneratePage1(id, nextBm.Item2);
                            var page2 = HidCommunicationHelper.GeneratePage2(id, nextBm.Item2);

                            device.Write(page1);
                            device.Write(page2);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }

            backgroundTasks[numberOfWriterThreads] = Task.Factory.StartNew(() =>
            {
                var cancelToken = threadCancelSource.Token;

                while (true)
                {
                    var rep = device.ReadReport();
                    if (cancelToken.IsCancellationRequested) return;
                    ProcessNewStates(rep.Data);
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void Device_Removed()
        {
            var arg = new ConnectionEventArgs(false);
            ConnectionStateChanged?.Invoke(this, arg);
        }

        private void Device_Inserted()
        {
            var arg = new ConnectionEventArgs(true);
            ConnectionStateChanged?.Invoke(this, arg);
        }

        private void VerifyNotDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(HidClient));
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
                    KeyStateChanged?.Invoke(this, new KeyEventArgs(i, newStates[i] != 0));
                    keyStates[i] = newStates[i];
                }
            }
        }

        public void SetBrightness(byte percent)
        {
            VerifyNotDisposed();
            device.WriteFeatureData(HidCommunicationHelper.GetBrightnessMsg(percent));
        }

        public void ShowLogo()
        {
            VerifyNotDisposed();
            ShowLogoWithoutDisposeVerification();
        }

        private void ShowLogoWithoutDisposeVerification()
        {
            device.WriteFeatureData(HidCommunicationHelper.ShowLogoMsg);
        }
    }

}
