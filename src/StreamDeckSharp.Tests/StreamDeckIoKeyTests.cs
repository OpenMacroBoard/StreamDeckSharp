using OpenMacroBoard.Meta.TestUtils;
using StreamDeckSharp.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace StreamDeckSharp.Tests
{
    [UsesVerify]
    public class StreamDeckIoKeyTests
    {
        public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

        public static IEnumerable<object[]> GetReportTestData()
        {
            // Unpack report and map for xunit injection use.
            return GetData().Select(x =>
                new object[]
                {
                    x.TestName,
                    x.Hardware,
                    Unpack(x.InputReports, x.Hardware.Driver.ExpectedInputReportLength),
                }
            );
        }

        [Theory]
        [MemberData(nameof(GetReportTestData))]
        internal async Task InputReportsBehaveAsExpected(
            string testName,
            UsbHardwareIdAndDriver hardware,
            IEnumerable<byte[]> inputReports
        )
        {
            // Arrange
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(testName);

            using var context = new StreamDeckHidTestContext(hardware);

            var keyLog = new StringBuilder();

            context.Board.KeyStateChanged += (s, e)
                => keyLog.Append(e.Key).Append(" - ").AppendLine(e.IsDown ? "DOWN" : "UP");

            // Act
            foreach (var report in inputReports)
            {
                context.Hid.FakeIncommingInputReport(report);
            }

            // Assert
            await Verifier.VerifyAsync(keyLog.ToString());
        }

        private static IEnumerable<KeyPressTestCase> GetData()
        {
            // Because most of the data in input reports is zero, the test reports are "packed"
            // with a simple method. Each byte is encoded in pairs:
            // [indexA], [valueA], [indexB], [valueB], [indexC], [valueC],...
            // The total size of the array is defined by the hardware information.

            // Some of the data has still a lot of redundancy in the packed form,
            // but are intentionally not "packed" more with loops to keep them simple and readable.

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeckXL_EachKeyOnce",
                Hardware = Hardware.StreamDeckXL.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 2, 32, 4, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 5, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 6, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 7, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 8, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 9, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 10, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 11, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 12, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 13, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 14, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 15, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 16, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 17, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 18, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 19, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 20, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 21, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 22, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 23, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 24, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 25, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 26, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 27, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 28, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 29, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 30, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 31, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 32, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 33, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 34, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 35, 1 },
                    new byte[] { 0, 1, 2, 32 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeckXL_MultipleKeysDown",
                Hardware = Hardware.StreamDeckXL.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 2, 32, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 2, 32, 4, 1, 5, 1, 6, 1 },
                    new byte[] { 0, 1, 2, 32, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 2, 32, 4, 1 },
                    new byte[] { 0, 1, 2, 32 },
                    new byte[] { 0, 1, 2, 32, 4, 1 },
                    new byte[] { 0, 1, 2, 32, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 2, 32, 4, 1, 5, 1, 6, 1 },
                    new byte[] { 0, 1, 2, 32, 5, 1, 6, 1 },
                    new byte[] { 0, 1, 2, 32, 6, 1 },
                    new byte[] { 0, 1, 2, 32 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeckMK2_EachKeyOnce",
                Hardware = Hardware.StreamDeckMK2.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 5, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 6, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 7, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 8, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 9, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 10, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 11, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 12, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 13, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 14, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 15, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 16, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 17, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 18, 1 },
                    new byte[] { 0, 1, 2, 15 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeckMK2_MultipleKeysDown",
                Hardware = Hardware.StreamDeckMK2.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 2, 15, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 2, 15, 4, 1, 5, 1, 6, 1 },
                    new byte[] { 0, 1, 2, 15, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 2, 15, 4, 1 },
                    new byte[] { 0, 1, 2, 15 },
                    new byte[] { 0, 1, 2, 15, 4, 1 },
                    new byte[] { 0, 1, 2, 15, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 2, 15, 4, 1, 5, 1, 6, 1 },
                    new byte[] { 0, 1, 2, 15, 5, 1, 6, 1 },
                    new byte[] { 0, 1, 2, 15, 6, 1 },
                    new byte[] { 0, 1, 2, 15 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeck_EachKeyOnce",
                Hardware = Hardware.StreamDeck.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 5, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 4, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 3, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 2, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 1, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 10, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 9, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 8, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 7, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 6, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 15, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 14, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 13, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 12, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 11, 1 },
                    new byte[] { 0, 1 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeck_MultipleKeysDown",
                Hardware = Hardware.StreamDeck.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 3, 1, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 5, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 5, 1 },
                    new byte[] { 0, 1, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 3, 1, 4, 1, 5, 1 },
                    new byte[] { 0, 1, 3, 1, 4, 1 },
                    new byte[] { 0, 1, 3, 1 },
                    new byte[] { 0, 1 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeckMini_EachKeyOnce",
                Hardware = Hardware.StreamDeckMini.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 2, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 3, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 4, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 5, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 6, 1 },
                    new byte[] { 0, 1 },
                },
            };

            yield return new KeyPressTestCase()
            {
                TestName = "StreamDeckMini_MultipleKeysDown",
                Hardware = Hardware.StreamDeckMini.Internal(),
                InputReports = new List<byte[]>
                {
                    new byte[] { 0, 1, 1, 1, 2, 1 },
                    new byte[] { 0, 1, 1, 1, 2, 1, 3, 1 },
                    new byte[] { 0, 1, 1, 1, 2, 1 },
                    new byte[] { 0, 1, 1, 1 },
                    new byte[] { 0, 1 },
                    new byte[] { 0, 1, 1, 1 },
                    new byte[] { 0, 1, 1, 1, 2, 1 },
                    new byte[] { 0, 1, 1, 1, 2, 1, 3, 1 },
                    new byte[] { 0, 1, 2, 1, 3, 1 },
                    new byte[] { 0, 1, 3, 1 },
                    new byte[] { 0, 1 },
                },
            };
        }

        private static IEnumerable<byte[]> Unpack(IEnumerable<byte[]> packedReports, int reportSize)
        {
            return packedReports.Select(x => Unpack(x, reportSize));
        }

        private static byte[] Unpack(byte[] packedReport, int reportSize)
        {
            var unpacked = new byte[reportSize];

            for (int i = 0; i < packedReport.Length; i += 2)
            {
                unpacked[packedReport[i]] = packedReport[i + 1];
            }

            return unpacked;
        }

        private class KeyPressTestCase
        {
            public string TestName { get; set; }
            public IEnumerable<byte[]> InputReports { get; set; }
            public UsbHardwareIdAndDriver Hardware { get; set; }
        }
    }
}
