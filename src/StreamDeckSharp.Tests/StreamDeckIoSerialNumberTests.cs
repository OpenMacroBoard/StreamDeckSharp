using FluentAssertions;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class StreamDeckIoSerialNumberTests
    {
        [Theory]
        [MemberData(nameof(TestData))]
        internal void GettingSerialNumberWorksAsExpected(
            IUsbHidHardware hardware,
            byte[] featureData,
            string expectedParsedSerialNumber
        )
        {
            var internalHardware = hardware.Internal();

            // Arrange
            using var context = new StreamDeckHidTestContext(internalHardware);

            context.Hid.ReadFeatureResonseQueue.Enqueue((internalHardware.Driver.SerialNumberFeatureId, true, featureData));

            // Act
            context.Board.GetSerialNumber().Should().Be(expectedParsedSerialNumber);

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
                    0x03, 0x55, 0xAA, 0xD3, 0x03, 0x41, 0x4C, 0x31,
                    0x35, 0x47, 0x31, 0x41, 0x30, 0x30, 0x36, 0x34,
                    0x36,
                },
                "AL15G1A00646"
            },
            {
                Hardware.StreamDeckXL,
                new byte[]
                {
                    0x06, 0x0C, 0x43, 0x4C, 0x31, 0x35, 0x4B, 0x31,
                    0x41, 0x30, 0x30, 0x31, 0x32, 0x38, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "CL15K1A00128"
            },
            {
                Hardware.StreamDeckMK2,
                new byte[]
                {
                    0x06, 0x0C, 0x44, 0x4C, 0x33, 0x30, 0x4B, 0x31,
                    0x41, 0x37, 0x39, 0x37, 0x34, 0x38, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "DL30K1A79748"
            },
            {
                Hardware.StreamDeckMini,
                new byte[]
                {
                    0x03, 0x00, 0x00, 0x00, 0x00, 0x42, 0x4C, 0x31,
                    0x39, 0x48, 0x31, 0x41, 0x30, 0x34, 0x37, 0x32,
                    0x34,
                },
                "BL19H1A04724"
            },
        };
    }
}
