using FluentAssertions;
using StreamDeckSharp.Internals;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class StreamDeckIoFirmwareTests
    {
        [Theory]
        [MemberData(nameof(GetData))]
        internal void GettingFirmwareVersionWorksAsExpected(
            UsbHardwareIdAndDriver hardware,
            byte[] featureData,
            string expectedParsedFirmware
        )
        {
            // Arrange
            using var context = new StreamDeckHidTestContext(hardware);

            context.Hid.ReadFeatureResonseQueue.Enqueue((hardware.Driver.FirmwareVersionFeatureId, true, featureData));

            // Act
            context.Board.GetFirmwareVersion().Should().Be(expectedParsedFirmware);

            // Assert
            context.Hid.ReadFeatureResonseQueue.Should().BeEmpty();
            context.Log.ToString().Should().BeEmpty();
        }

        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Readability")]
        public static IEnumerable<object[]> GetData()
        {
            // Real world examples from my devices

            yield return new object[]
            {
                Hardware.StreamDeck,
                new byte[]
                {
                    0x04, 0x55, 0xAA, 0xD4, 0x04, 0x31, 0x2E, 0x30,
                    0x2E, 0x31, 0x39, 0x31, 0x32, 0x30, 0x33, 0x00,
                    0x00,
                },
                "1.0.191203",
            };

            yield return new object[]
            {
                Hardware.StreamDeckXL,
                new byte[]
                {
                    0x05, 0x0C, 0xAC, 0x74, 0x1D, 0x08, 0x31, 0x2E,
                    0x30, 0x30, 0x2E, 0x30, 0x30, 0x36, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "1.00.006",
            };

            yield return new object[]
            {
                Hardware.StreamDeckMK2,
                new byte[]
                {
                    0x05, 0x0C, 0x9F, 0x8E, 0x29, 0xE3, 0x31, 0x2E,
                    0x30, 0x30, 0x2E, 0x30, 0x30, 0x31, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "1.00.001",
            };

            yield return new object[]
            {
                Hardware.StreamDeckMini,
                new byte[]
                {
                    0x04, 0x00, 0x00, 0x00, 0x00, 0x32, 0x2E, 0x30,
                    0x33, 0x2E, 0x30, 0x30, 0x31, 0x00, 0x00, 0x00,
                    0x00,
                },
                "2.03.001",
            };
        }
    }
}
