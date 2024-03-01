using FluentAssertions;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class StreamDeckIoFirmwareTests
    {
        [Theory]
        [MemberData(nameof(TestData))]
        public void GettingFirmwareVersionWorksAsExpected(
            IUsbHidHardware hardware,
            byte[] featureData,
            string expectedParsedFirmware
        )
        {
            var internalHardware = hardware.Internal();

            // Arrange
            using var context = new StreamDeckHidTestContext(internalHardware);

            context.Hid.ReadFeatureResonseQueue.Enqueue((internalHardware.Driver.FirmwareVersionFeatureId, true, featureData));

            // Act
            context.Board.GetFirmwareVersion().Should().Be(expectedParsedFirmware);

            // Assert
            context.Hid.ReadFeatureResonseQueue.Should().BeEmpty();
            context.Log.ToString().Should().BeEmpty();
        }

        /// <summary>
        /// Real world examples from my devices.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test before test-data.")]
        public static TheoryData<IUsbHidHardware, byte[], string> TestData { get; } = new()
        {
            {
                Hardware.StreamDeck,
                new byte[]
                {
                    0x04, 0x55, 0xAA, 0xD4, 0x04, 0x31, 0x2E, 0x30,
                    0x2E, 0x31, 0x39, 0x31, 0x32, 0x30, 0x33, 0x00,
                    0x00,
                },
                "1.0.191203"
            },
            {
                Hardware.StreamDeckXL,
                new byte[]
                {
                    0x05, 0x0C, 0xAC, 0x74, 0x1D, 0x08, 0x31, 0x2E,
                    0x30, 0x30, 0x2E, 0x30, 0x30, 0x36, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "1.00.006"
            },
            {
                Hardware.StreamDeckMK2,
                new byte[]
                {
                    0x05, 0x0C, 0x9F, 0x8E, 0x29, 0xE3, 0x31, 0x2E,
                    0x30, 0x30, 0x2E, 0x30, 0x30, 0x31, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "1.00.001"
            },
            {
                Hardware.StreamDeckMini,
                new byte[]
                {
                    0x04, 0x00, 0x00, 0x00, 0x00, 0x32, 0x2E, 0x30,
                    0x33, 0x2E, 0x30, 0x30, 0x31, 0x00, 0x00, 0x00,
                    0x00,
                },
                "2.03.001"
            },
        };
    }
}
