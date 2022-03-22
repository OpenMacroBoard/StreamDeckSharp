using OpenMacroBoard.SDK;
using System;
using System.Text;
using System.Threading;

namespace StreamDeckSharp.Internals
{
    internal class BasicHidClient : IMacroBoard
    {
        private readonly byte[] keyStates;
        private readonly object disposeLock = new();

        public BasicHidClient(IStreamDeckHid deckHid, IHardwareInternalInfos hardwareInformation)
        {
            DeckHid = deckHid;
            Keys = hardwareInformation.Keys;

            deckHid.ConnectionStateChanged += (s, e) => ConnectionStateChanged?.Invoke(this, e);
            deckHid.ReportReceived += DeckHid_ReportReceived;

            HardwareInfo = hardwareInformation;
            Buffer = new byte[deckHid.OutputReportLength];
            keyStates = new byte[Keys.Count];
        }

        public event EventHandler<KeyEventArgs> KeyStateChanged;
        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;

        public IKeyLayout Keys { get; }
        public bool IsDisposed { get; private set; }
        public bool IsConnected => DeckHid.IsConnected;

        protected IStreamDeckHid DeckHid { get; }
        protected IHardwareInternalInfos HardwareInfo { get; }
        protected byte[] Buffer { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string GetFirmwareVersion()
        {
            return ReadFeatureString(HardwareInfo.FirmwareVersionFeatureId, HardwareInfo.FirmwareReportSkip);
        }

        public string GetSerialNumber()
        {
            return ReadFeatureString(HardwareInfo.SerialNumberFeatureId, HardwareInfo.SerialNumberReportSkip);
        }

        public void SetBrightness(byte percent)
        {
            ThrowIfAlreadyDisposed();
            DeckHid.WriteFeature(HardwareInfo.GetBrightnessMessage(percent));
        }

        public virtual void SetKeyBitmap(int keyId, KeyBitmap bitmapData)
        {
            keyId = HardwareInfo.ExtKeyIdToHardwareKeyId(keyId);

            var payload = HardwareInfo.GeneratePayload(bitmapData);

            var reports = OutputReportSplitter.Split(
                payload,
                Buffer,
                HardwareInfo.ReportSize,
                HardwareInfo.HeaderSize,
                keyId,
                HardwareInfo.PrepareDataForTransmittion
            );

            foreach (var report in reports)
            {
                DeckHid.WriteReport(report);
            }
        }

        public void ShowLogo()
        {
            ThrowIfAlreadyDisposed();
            ShowLogoWithoutDisposeVerification();
        }

        protected virtual void Shutdown()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (disposeLock)
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    IsDisposed = true;
                }

                Shutdown();

                // Sleep to let the stream deck catch up.
                // Without this Sleep() the stream deck might set a key image after the logo was shown.
                // I've no idea why it's sometimes executed out of order even though the write is synchronized.
                Thread.Sleep(50);

                ShowLogoWithoutDisposeVerification();

                DeckHid.Dispose();
            }
        }

        protected void ThrowIfAlreadyDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(BasicHidClient));
            }
        }

        private string ReadFeatureString(byte featureId, int skipBytes)
        {
            if (!DeckHid.ReadFeatureData(featureId, out var featureData))
            {
                return null;
            }

            return Encoding.UTF8.GetString(featureData, skipBytes, featureData.Length - skipBytes).Trim('\0');
        }

        private void DeckHid_ReportReceived(object sender, ReportReceivedEventArgs e)
        {
            ProcessKeys(e.ReportData);
        }

        private void ProcessKeys(byte[] newStates)
        {
            for (var i = 0; i < keyStates.Length; i++)
            {
                var newStatePos = i + HardwareInfo.KeyReportOffset;

                if (keyStates[i] != newStates[newStatePos])
                {
                    var externalKeyId = HardwareInfo.HardwareKeyIdToExtKeyId(i);
                    KeyStateChanged?.Invoke(this, new KeyEventArgs(externalKeyId, newStates[newStatePos] != 0));
                    keyStates[i] = newStates[newStatePos];
                }
            }
        }

        private void ShowLogoWithoutDisposeVerification()
        {
            DeckHid.WriteFeature(HardwareInfo.GetLogoMessage());
        }
    }
}
