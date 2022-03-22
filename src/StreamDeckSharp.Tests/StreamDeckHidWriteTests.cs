using OpenMacroBoard.Meta.TestUtils;
using StreamDeckSharp.Internals;
using System.Collections.Generic;
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

        public static IEnumerable<object[]> GetDataForBrightnessTest()
        {
            var brighnessValues = new byte[] { 100, 0, 33, 66 };

            return HardwareInfoResolver
                .GetAllHardwareInfos()
                .CrossRef(brighnessValues)
                .Select(x => new object[] { x.Item1, x.Item2 });
        }

        [Theory]
        [MemberData(nameof(GetDataForBrightnessTest))]
        internal async Task SettingBrightnessCausesTheExpectedOuput(IHardwareInternalInfos hardware, byte brightness)
        {
            // Arrange
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(hardware.DeviceName)
                .UseUniqueSuffix($"Value={brightness}");

            using var context = new StreamDeckHidTestContext(hardware);

            // Act
            context.Board.SetBrightness(brightness);

            // Assert
            await Verifier.VerifyAsync(context.Log.ToString());
        }

        [Theory]
        [ClassData(typeof(AllHardwareInfoTestData))]
        internal async Task CallingShowLogoCausesTheExpectedOutput(IHardwareInternalInfos hardware)
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
