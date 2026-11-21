using AwesomeAssertions;
using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace StreamDeckSharp.Tests;

public class StreamDeckJpegEncoderTests
{
    [Fact]
    public void DefaultOptionsUseTheBuiltInEncoderAndTheWriteCache()
    {
        StreamDeckOptions.Default.JpegEncoder.Should().BeSameAs(OmbJpegEncoder.Default);
        StreamDeckOptions.Default.UseWriteCache.Should().BeTrue();
    }

    [Fact]
    public void CustomJpegEncoderIsUsedForJpegDevices()
    {
        // Arrange
        var hardware = Hardware.StreamDeckXL.Internal();
        var encoder = new RecordingJpegEncoder();

        using var log = new StringWriter();
        var hid = new FakeStreamDeckHid(log, hardware);
        using var board = new BasicHidClient(hid, hardware.Keys, hardware.Driver, encoder);

        // Act
        board.SetKeyBitmap(0, KeyBitmap.Create.FromRgb(0xFF, 0, 0));

        // Assert
        // The 1x1 bitmap was scaled to the key size of the device before it was encoded.
        var keySize = hardware.Driver.KeyImageSize;
        encoder.Calls.Should().Equal((keySize, keySize, keySize * keySize * 3));

        log.ToString().Should().Contain("0xCA 0xFE 0xBA 0xBE");
    }

    [Fact]
    public void JpegEncoderIsNotUsedForBitmapDevices()
    {
        // Arrange
        var hardware = Hardware.StreamDeck.Internal();
        var encoder = new RecordingJpegEncoder();

        using var log = new StringWriter();
        var hid = new FakeStreamDeckHid(log, hardware);
        using var board = new BasicHidClient(hid, hardware.Keys, hardware.Driver, encoder);

        // Act
        board.SetKeyBitmap(0, KeyBitmap.Create.FromRgb(0xFF, 0, 0));

        // Assert
        encoder.Calls.Should().BeEmpty();
    }

    [Theory]
    [ClassData(typeof(JpegHardwareInfoTestData))]
    internal void JpegPayloadDecodesToTheRotatedKeyImage(UsbHardwareIdAndDriver hardware)
    {
        // Arrange
        var keySize = hardware.Driver.KeyImageSize;
        var source = CreateTiles(keySize, 42);

        // Act
        var payload = hardware.Driver.GeneratePayload(source, OmbJpegEncoder.Default);

        // Assert
        var decoded = OmbImageDecoder.Default.Decode(payload);

        decoded.Width.Should().Be(keySize);
        decoded.Height.Should().Be(keySize);

        var decodedPixels = GetData(decoded);

        // The devices show the image upside down, so the payload is rotated by 180 degrees.
        var errorRotated = MeanAbsoluteDifference(decodedPixels, GetData(source.Rotate180()));
        var errorNotRotated = MeanAbsoluteDifference(decodedPixels, GetData(source));

        errorRotated.Should().BeLessThan(8.0);
        errorNotRotated.Should().BeGreaterThan(40.0);
    }

    /// <summary>
    /// Creates a square bitmap of 8x8 pixel tiles with random colors.
    /// </summary>
    private static KeyBitmap CreateTiles(int size, int seed)
    {
        const int tileSize = 8;

        var random = new Random(seed);
        var color = new byte[3];
        var pixels = new byte[size * size * 3];

        for (var tileY = 0; tileY < size; tileY += tileSize)
        {
            for (var tileX = 0; tileX < size; tileX += tileSize)
            {
                random.NextBytes(color);

                for (var y = tileY; y < Math.Min(size, tileY + tileSize); y++)
                {
                    for (var x = tileX; x < Math.Min(size, tileX + tileSize); x++)
                    {
                        color.CopyTo(pixels, ((y * size) + x) * 3);
                    }
                }
            }
        }

        return KeyBitmap.Create.FromBgr24Array(size, size, pixels);
    }

    private static byte[] GetData(KeyBitmap bitmap)
    {
        return ((IKeyBitmapDataAccess)bitmap).GetData().ToArray();
    }

    private static double MeanAbsoluteDifference(byte[] left, byte[] right)
    {
        left.Should().HaveCount(right.Length);

        long sum = 0;

        for (var i = 0; i < left.Length; i++)
        {
            sum += Math.Abs(left[i] - right[i]);
        }

        return (double)sum / left.Length;
    }

    private sealed class RecordingJpegEncoder : IJpegEncoder
    {
        public List<(int Width, int Height, int Length)> Calls { get; } = [];

        public byte[] Encode(ReadOnlySpan<byte> bitmapDataBgr24, int width, int height)
        {
            Calls.Add((width, height, bitmapDataBgr24.Length));
            return [0xCA, 0xFE, 0xBA, 0xBE];
        }
    }
}
