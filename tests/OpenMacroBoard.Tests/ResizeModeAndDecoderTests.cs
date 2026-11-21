using AwesomeAssertions;
using OpenMacroBoard.SDK;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace OpenMacroBoard.Tests;

public class ResizeModeAndDecoderTests
{
    private const byte White = 255;
    private const byte Black = 0;

    [Fact]
    public void PadScalesToFitAndFillsTheRestWithBlack()
    {
        // a wide white bitmap in a square target: black bars above and below
        var result = CreateSingleColor(8, 4, White).Resize(4, 4, OmbResizeMode.Pad);

        GetRows(result).Should().Equal("....", "####", "####", "....");
    }

    [Fact]
    public void BoxPadCentersASmallBitmapWithoutScaling()
    {
        var result = CreateSingleColor(2, 2, White).Resize(6, 4, OmbResizeMode.BoxPad);

        GetRows(result).Should().Equal("......", "..##..", "..##..", "......");
    }

    [Fact]
    public void BoxPadScalesALargeBitmapLikePad()
    {
        var source = CreateSingleColor(8, 4, White);

        var boxPad = ImageDecoderTests.GetData(source.Resize(4, 4, OmbResizeMode.BoxPad));
        var pad = ImageDecoderTests.GetData(source.Resize(4, 4, OmbResizeMode.Pad));

        boxPad.Should().Equal(pad);
    }

    [Fact]
    public void CropCoversTheTargetAndCutsOffBothSides()
    {
        // white in the two middle columns, black on both sides
        var pixels = new byte[4 * 2 * 3];

        for (var y = 0; y < 2; y++)
        {
            for (var x = 1; x <= 2; x++)
            {
                pixels.AsSpan(((y * 4) + x) * 3, 3).Fill(White);
            }
        }

        var result = KeyBitmap.Create.FromBgr24Array(4, 2, pixels).Resize(2, 2, OmbResizeMode.Crop);

        GetRows(result).Should().Equal("##", "##");
    }

    [Fact]
    public void StretchIgnoresTheProportions()
    {
        var result = CreateSingleColor(8, 2, White).Resize(3, 5, OmbResizeMode.Stretch);

        GetRows(result).Should().Equal("###", "###", "###", "###", "###");
    }

    [Fact]
    public void UnknownResizeModeIsRejected()
    {
        Action act = () => CreateSingleColor(2, 2, White).Resize(4, 4, (OmbResizeMode)99);

        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("resizeMode");
    }

    [Fact]
    public async Task CustomDecoderIsUsedByAllOverloads()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var decoder = new RecordingDecoder();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");

        try
        {
            await File.WriteAllBytesAsync(path, content, TestContext.Current.CancellationToken);

            KeyBitmap.Create.FromStream(new MemoryStream(content), decoder).Should().BeSameAs(decoder.Result);
            (await KeyBitmap.Create.FromStreamAsync(new MemoryStream(content), decoder)).Should().BeSameAs(decoder.Result);
            KeyBitmap.Create.FromFile(path, decoder).Should().BeSameAs(decoder.Result);
            (await KeyBitmap.Create.FromFileAsync(path, decoder)).Should().BeSameAs(decoder.Result);
        }
        finally
        {
            File.Delete(path);
        }

        decoder.Calls.Should().Be(4);
        decoder.LastContent.Should().Equal(content);
    }

    [Fact]
    public void BuiltInDecoderGivesTheSameResultAsFromStream()
    {
        var jpeg = AssetLoader.LoadBytes("Jpeg.base_420.jpg");

        var direct = OmbImageDecoder.Default.Decode(jpeg);
        var fromStream = KeyBitmap.Create.FromStream(new MemoryStream(jpeg));

        direct.Width.Should().Be(fromStream.Width);
        direct.Height.Should().Be(fromStream.Height);
        ImageDecoderTests.GetData(direct).Should().Equal(ImageDecoderTests.GetData(fromStream));
    }

    private static KeyBitmap CreateSingleColor(int width, int height, byte value)
    {
        var pixels = new byte[width * height * 3];
        pixels.AsSpan().Fill(value);
        return KeyBitmap.Create.FromBgr24Array(width, height, pixels);
    }

    /// <summary>
    /// Renders a black and white bitmap as text: '#' for white and '.' for black pixels.
    /// </summary>
    private static string[] GetRows(KeyBitmap bitmap)
    {
        var data = ImageDecoderTests.GetData(bitmap);
        var rows = new string[bitmap.Height];

        for (var y = 0; y < bitmap.Height; y++)
        {
            var row = new char[bitmap.Width];

            for (var x = 0; x < bitmap.Width; x++)
            {
                var value = data[((y * bitmap.Width) + x) * 3];
                row[x] = value == White ? '#' : value == Black ? '.' : '?';
            }

            rows[y] = new string(row);
        }

        return rows;
    }

    private sealed class RecordingDecoder : IImageDecoder
    {
        public KeyBitmap Result { get; } = KeyBitmap.Create.FromRgb(1, 2, 3);

        public int Calls { get; private set; }

        public byte[] LastContent { get; private set; } = [];

        public KeyBitmap Decode(ReadOnlySpan<byte> encodedImage)
        {
            Calls++;
            LastContent = encodedImage.ToArray();
            return Result;
        }
    }
}
