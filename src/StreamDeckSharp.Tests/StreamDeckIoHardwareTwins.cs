using Newtonsoft.Json;
using OpenMacroBoard.Meta.TestUtils;
using StreamDeckSharp.Internals;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace StreamDeckSharp.Tests
{
    [UsesVerify]
    public class StreamDeckIoHardwareTwins
    {
        public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

        [Theory]
        [ClassData(typeof(AllHardwareInfoTestData))]
        internal async Task HardwareTwinsHaveExpectedValues(UsbHardwareIdAndDriver hardware)
        {
            // This test is to make sure we don't accidentially change some important constants.
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(hardware.DeviceName)
                ;

            var hardwareJson = JsonConvert.SerializeObject(hardware);
            await Verifier.VerifyJsonAsync(hardwareJson);
        }
    }
}
