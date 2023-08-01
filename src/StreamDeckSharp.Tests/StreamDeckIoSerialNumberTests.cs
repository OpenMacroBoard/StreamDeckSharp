using FluentAssertions;
using StreamDeckSharp.Internals;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace StreamDeckSharp.Tests
{
    public class StreamDeckIoSerialNumberTests
    {
        [Theory]
        [MemberData(nameof(GetData))]
        internal void GettingSerialNumberWorksAsExpected(
            UsbHardwareIdAndDriver hardware,
            byte[] featureData,
            string expectedParsedSerialNumber
        )
        {
            // Arrange
            using var context = new StreamDeckHidTestContext(hardware);

            context.Hid.ReadFeatureResonseQueue.Enqueue((hardware.Driver.SerialNumberFeatureId, true, featureData));

            // Act
            context.Board.GetSerialNumber().Should().Be(expectedParsedSerialNumber);

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
                    0x03, 0x55, 0xAA, 0xD3, 0x03, 0x41, 0x4C, 0x31,
                    0x35, 0x47, 0x31, 0x41, 0x30, 0x30, 0x36, 0x34,
                    0x36,
                },
                "AL15G1A00646",
            };

            yield return new object[]
            {
                Hardware.StreamDeckXL,
                new byte[]
                {
                    0x06, 0x0C, 0x43, 0x4C, 0x31, 0x35, 0x4B, 0x31,
                    0x41, 0x30, 0x30, 0x31, 0x32, 0x38, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "CL15K1A00128",
            };

            yield return new object[]
            {
                Hardware.StreamDeckMK2,
                new byte[]
                {
                    0x06, 0x0C, 0x44, 0x4C, 0x33, 0x30, 0x4B, 0x31,
                    0x41, 0x37, 0x39, 0x37, 0x34, 0x38, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                },
                "DL30K1A79748",
            };

            yield return new object[]
            {
                Hardware.StreamDeckMini,
                new byte[]
                {
                    0x03, 0x00, 0x00, 0x00, 0x00, 0x42, 0x4C, 0x31,
                    0x39, 0x48, 0x31, 0x41, 0x30, 0x34, 0x37, 0x32,
                    0x34,
                },
                "BL19H1A04724",
            };
        }
    }
}
