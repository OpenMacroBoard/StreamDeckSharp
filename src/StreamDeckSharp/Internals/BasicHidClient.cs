using OpenMacroBoard.SDK;
using System;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckSharp.Internals
{
    internal class BasicHidClient : IStreamDeckBoard
    {
        protected readonly byte[] buffer;
        private readonly byte[] keyStates;
        private readonly object disposeLock = new object();
        protected readonly IHardwareInternalInfos hardwareInformation;

        private readonly Task keyPollingTask;
        protected IStreamDeckHid deckHid;

        public BasicHidClient(IStreamDeckHid deckHid, IHardwareInternalInfos hardwareInformation)
        {
            this.deckHid = deckHid;
            Keys = hardwareInformation.Keys;
            deckHid.ConnectionStateChanged += (s, e) => ConnectionStateChanged?.Invoke(this, e);
            this.hardwareInformation = hardwareInformation;
            buffer = new byte[deckHid.OutputReportLength];
            keyStates = new byte[Keys.Count];

            keyPollingTask = StartKeyPollingTask();
        }

        public GridKeyPositionCollection Keys { get; }
        IKeyPositionCollection IMacroBoard.Keys => Keys;

        public bool IsDisposed { get; private set; }
        public bool IsConnected => deckHid.IsConnected;
        public event EventHandler<KeyEventArgs> KeyStateChanged;
        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        public void Dispose()
        {
            lock (disposeLock)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;
            }

            Shutdown();
            ShowLogoWithoutDisposeVerification();

            deckHid.Dispose();
            Task.WaitAll(keyPollingTask);

            Dispose(true);
        }

        protected virtual void Shutdown() { }
        protected virtual void Dispose(bool managed) { }

        public string GetFirmwareVersion()
        {
            var featureData = deckHid.ReadFeatureData(4);
            return Encoding.UTF8.GetString(featureData, 5, featureData.Length - 5).Trim('\0');
        }

        public string GetSerialNumber()
        {
            var featureData = deckHid.ReadFeatureData(3);
            return Encoding.UTF8.GetString(featureData, 5, featureData.Length - 5).Trim('\0');
        }

        public void SetBrightness(byte percent)
        {
            VerifyNotDisposed();
            deckHid.WriteFeature(GetBrightnessMsg(percent));
        }

        public virtual void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            keyId = hardwareInformation.ExtKeyIdToHardwareKeyId(keyId);

            var payload = hardwareInformation.GeneratePayload(bitmapData);

            foreach (var report in OutputReportSplitter.Split(payload, buffer, hardwareInformation.ReportSize, hardwareInformation.HeaderSize, keyId, hardwareInformation.PrepareDataForTransmittion))
                deckHid.WriteReport(report);
        }

        public void ShowLogo()
        {
            VerifyNotDisposed();
            ShowLogoWithoutDisposeVerification();
        }

        protected void VerifyNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(BasicHidClient));
        }

        public void ProcessKeys()
        {
            var newStates = deckHid.ReadReport();

            for (int i = 0; i < keyStates.Length; i++)
            {
                var newStatePos = i + hardwareInformation.KeyReportOffset;

                if (keyStates[i] != newStates[newStatePos])
                {
                    var externalKeyId = hardwareInformation.HardwareKeyIdToExtKeyId(i);
                    KeyStateChanged?.Invoke(this, new KeyEventArgs(externalKeyId, newStates[newStatePos] != 0));
                    keyStates[i] = newStates[newStatePos];
                }
            }
        }

        private Task StartKeyPollingTask()
        {
            return Task.Factory.StartNew(() =>
            {
                while (!IsDisposed)
                    ProcessKeys();
            }, TaskCreationOptions.LongRunning);
        }

        private void ShowLogoWithoutDisposeVerification()
        {
            deckHid.WriteFeature(showLogoMsg);
        }

        private static byte[] GetBrightnessMsg(byte percent)
        {
            if (percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            var buffer = new byte[] { 0x05, 0x55, 0xaa, 0xd1, 0x01, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            buffer[5] = percent;
            return buffer;
        }

        private static readonly byte[] showLogoMsg = new byte[] { 0x0B, 0x63 };
    }
}
