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
        protected readonly IHardwareInternalInfos hwInfo;

        private readonly Task keyPollingTask;
        protected IStreamDeckHid deckHid;

        public BasicHidClient(IStreamDeckHid deckHid, IHardwareInternalInfos hardwareInformation)
        {
            this.deckHid = deckHid;
            Keys = hardwareInformation.Keys;
            deckHid.ConnectionStateChanged += (s, e) => ConnectionStateChanged?.Invoke(this, e);
            this.hwInfo = hardwareInformation;
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
            var featureData = deckHid.ReadFeatureData(hwInfo.FirmwareVersionFeatureId);
            return Encoding.UTF8.GetString(featureData, hwInfo.FirmwareReportSkip, featureData.Length - hwInfo.FirmwareReportSkip).Trim('\0');
        }

        public string GetSerialNumber()
        {
            var featureData = deckHid.ReadFeatureData(hwInfo.SerialNumberFeatureId);
            return Encoding.UTF8.GetString(featureData, hwInfo.SerialNumberReportSkip, featureData.Length - hwInfo.SerialNumberReportSkip).Trim('\0');
        }

        public void SetBrightness(byte percent)
        {
            VerifyNotDisposed();
            deckHid.WriteFeature(hwInfo.GetBrightnessMessage(percent));
        }

        public virtual void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            keyId = hwInfo.ExtKeyIdToHardwareKeyId(keyId);

            var payload = hwInfo.GeneratePayload(bitmapData);

            foreach (var report in OutputReportSplitter.Split(payload, buffer, hwInfo.ReportSize, hwInfo.HeaderSize, keyId, hwInfo.PrepareDataForTransmittion))
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
                var newStatePos = i + hwInfo.KeyReportOffset;

                if (keyStates[i] != newStates[newStatePos])
                {
                    var externalKeyId = hwInfo.HardwareKeyIdToExtKeyId(i);
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
            deckHid.WriteFeature(hwInfo.GetLogoMessage());
        }
    }
}
