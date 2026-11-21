using System;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Scales BGR24 pixel data.
/// </summary>
/// <remarks>
/// <para>Every target pixel is a weighted average of the source pixels around it. When an image is
/// made smaller, the area that is averaged grows with the scale, so no source pixel is skipped.
/// When it is made larger, this is the same as bilinear interpolation.</para>
/// </remarks>
internal static class Bgr24Resampler
{
    private const int ChannelCount = 3;

    public static byte[] Resize(ReadOnlySpan<byte> source, int sourceWidth, int sourceHeight, int width, int height)
    {
        var horizontal = BuildWeights(sourceWidth, width);
        var vertical = BuildWeights(sourceHeight, height);

        // first pass: scale every row to the target width
        var rows = new double[checked(sourceHeight * width * ChannelCount)];

        for (var y = 0; y < sourceHeight; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var weights = horizontal[x];
                var target = ((y * width) + x) * ChannelCount;

                for (var i = 0; i < weights.Values.Length; i++)
                {
                    var from = ((y * sourceWidth) + weights.Start + i) * ChannelCount;
                    var weight = weights.Values[i];

                    rows[target] += source[from] * weight;
                    rows[target + 1] += source[from + 1] * weight;
                    rows[target + 2] += source[from + 2] * weight;
                }
            }
        }

        // second pass: scale every column to the target height
        var result = new byte[checked(width * height * ChannelCount)];
        Span<double> sums = stackalloc double[ChannelCount];

        for (var y = 0; y < height; y++)
        {
            var weights = vertical[y];

            for (var x = 0; x < width; x++)
            {
                sums.Clear();

                for (var i = 0; i < weights.Values.Length; i++)
                {
                    var from = (((weights.Start + i) * width) + x) * ChannelCount;
                    var weight = weights.Values[i];

                    sums[0] += rows[from] * weight;
                    sums[1] += rows[from + 1] * weight;
                    sums[2] += rows[from + 2] * weight;
                }

                var target = ((y * width) + x) * ChannelCount;

                result[target] = ToByte(sums[0]);
                result[target + 1] = ToByte(sums[1]);
                result[target + 2] = ToByte(sums[2]);
            }
        }

        return result;
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
    }

    /// <summary>
    /// For every target position: which source positions are averaged, and with which weights.
    /// </summary>
    private static Weights[] BuildWeights(int sourceSize, int targetSize)
    {
        var scale = (double)sourceSize / targetSize;

        // How far a source pixel may be away from the center to still count.
        var radius = Math.Max(1.0, scale);
        var result = new Weights[targetSize];

        for (var i = 0; i < targetSize; i++)
        {
            var center = ((i + 0.5) * scale) - 0.5;

            var first = Math.Max(0, (int)Math.Ceiling(center - radius));
            var last = Math.Min(sourceSize - 1, (int)Math.Floor(center + radius));

            // The center can lie outside of the image at the borders; then the closest pixel is used.
            first = Math.Min(first, sourceSize - 1);
            last = Math.Max(last, first);

            var values = new double[last - first + 1];
            var sum = 0.0;

            for (var j = first; j <= last; j++)
            {
                var weight = Math.Max(0.0, 1.0 - (Math.Abs(j - center) / radius));
                values[j - first] = weight;
                sum += weight;
            }

            if (sum <= 0.0)
            {
                // Can only happen with a single source pixel that lies exactly on the edge of the radius.
                values.AsSpan().Fill(1.0);
                sum = values.Length;
            }

            for (var j = 0; j < values.Length; j++)
            {
                values[j] /= sum;
            }

            result[i] = new Weights(first, values);
        }

        return result;
    }

    private readonly record struct Weights(int Start, double[] Values);
}
