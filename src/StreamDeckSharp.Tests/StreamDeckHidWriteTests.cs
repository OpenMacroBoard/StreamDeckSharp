using OpenMacroBoard.Meta.TestUtils;
using StreamDeckSharp.Internals;
using System.Linq;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace StreamDeckSharp.Tests
{
    [UsesVerify]
    public class StreamDeckHidWriteTests
    {
        public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

        public static TheoryData<IUsbHidHardware, byte> GetDataForBrightnessTest()
        {
            var brighnessValues = new byte[] { 100, 0, 33, 66 };

            return Hardware
                .GetInternalStreamDeckHardwareInfos()
                .Cast<IUsbHidHardware>()
                .CrossRef(brighnessValues)
                .ToTheoryData();
        }

        [Theory]
        [MemberData(nameof(GetDataForBrightnessTest))]
        internal async Task SettingBrightnessCausesTheExpectedOuput(
            IUsbHidHardware hardware,
            byte brightness
        )
        {
            var hardwareInternal = hardware.Internal();

            // Arrange
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(hardwareInternal.DeviceName)
                .UseUniqueSuffix($"Value={brightness}");

            using var context = new StreamDeckHidTestContext(hardwareInternal);

            // Act
            context.Board.SetBrightness(brightness);

            // Assert
            await Verifier.VerifyAsync(context.Log.ToString());
        }

        [Theory]
        [ClassData(typeof(AllHardwareInfoTestData))]
        internal async Task CallingShowLogoCausesTheExpectedOutput(UsbHardwareIdAndDriver hardware)
        {
            // Arrange
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(hardware.DeviceName);

            using var context = new StreamDeckHidTestContext(hardware);

            // Act
            context.Board.ShowLogo();

            // Assert
            await Verifier.VerifyAsync(context.Log.ToString());
        }
    }
}
