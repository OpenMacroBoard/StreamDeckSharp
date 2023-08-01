using FluentAssertions;
using OpenMacroBoard.Meta.TestUtils;
using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals;
using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace StreamDeckSharp.Tests
{
    [UsesVerify]
    public class StreamDeckIoBitmapKeyTests
    {
        public ExtendedVerifySettings Verifier { get; } = DefaultVerifySettings.Build();

        [Theory]
        [ClassData(typeof(AllHardwareInfoTestData))]
        internal async Task BasicBitmapKeyOutputAsExpected(UsbHardwareIdAndDriver hardware)
        {
            // Arrange
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(hardware.DeviceName)
                ;

            using var context = new StreamDeckHidTestContext(hardware);
            context.Hid.BytesPerLineOutput = 1024;

            var colorEachChannelDifferent = KeyBitmap.Create.FromRgb(0x6A, 0x4A, 0x4F);
            var red = KeyBitmap.Create.FromRgb(0xFF, 0, 0);
            var blue = KeyBitmap.Create.FromRgb(0, 0, 0xFF);

            // Act
            context.Log.WriteLine("Set all keys to a defined color.");
            context.Board.SetKeyBitmap(colorEachChannelDifferent);

            context.Log.WriteLine("Clear all keys.");
            context.Board.ClearKeys();

            context.Log.WriteLine("Set key 0 to red.");
            context.Board.SetKeyBitmap(0, red);

            context.Log.WriteLine("Clear key 0.");
            context.Board.ClearKey(0);

            context.Log.WriteLine("Set key 0 to blue");
            context.Board.SetKeyBitmap(0, blue);

            context.Log.WriteLine("Set key 0 to KeyBitmap.Black");
            context.Board.SetKeyBitmap(0, KeyBitmap.Black);

            // Assert
            await Verifier.VerifyAsync(context.Log.ToString());
        }

        [Theory]
        [ClassData(typeof(AllHardwareInfoTestData))]
        internal async Task SetBitmapResultsInExpectedOutput8PxTiles(UsbHardwareIdAndDriver hardware)
        {
            // Arrange
            Verifier.Initialize();

            Verifier
                .UseFileNameAsDirectory()
                .UseFileName(hardware.DeviceName)
                ;

            using var context = new StreamDeckHidTestContext(hardware);
            context.Hid.BytesPerLineOutput = 1024;

            // create an image that probably survives JPEG compression
            // by using 8x8 tiles that have the same color and hopefully results in similar JPEGs
            // even when using different JPEG encoders.
            // In a best case scenario I hope this test is resistant against swapping JPEG encoders.

            // we have to create a key bitmap for the exact key size to prevent automatic resizing.

            var keySize = hardware.Keys.KeySize;

            const int tileSize = 8;
            const int channelCount = 3;

            // make sure the key size is divisible by 8
            (keySize % tileSize).Should().Be(0);

            var tileCnt = keySize / tileSize;
            var rnd = new Random(42);
            var colorBuffer = new byte[3];

            (byte R, byte G, byte B) GetNextRndColor()
            {
                rnd.NextBytes(colorBuffer);
                return (colorBuffer[0], colorBuffer[1], colorBuffer[2]);
            }

            var pixelData = new byte[keySize * keySize * channelCount];

            for (int y = 0; y < tileCnt; y++)
            {
                for (int x = 0; x < tileCnt; x++)
                {
                    var (r, g, b) = GetNextRndColor();

                    for (int dy = 0; dy < tileSize; dy++)
                    {
                        for (int dx = 0; dx < tileSize; dx++)
                        {
                            var yOffset = (y * tileSize + dy) * keySize;
                            var xOffset = x * tileSize + dx;
                            var index = (yOffset + xOffset) * channelCount;

                            pixelData[index] = b;
                            pixelData[index + 1] = g;
                            pixelData[index + 2] = r;
                        }
                    }
                }
            }

            var colorFullKeyBitmap = KeyBitmap.Create.FromBgr24Array(keySize, keySize, pixelData);

            // Act
            context.Board.SetKeyBitmap(0, colorFullKeyBitmap);

            // Assert
            await Verifier.VerifyAsync(context.Log.ToString());
        }
    }
}
