using System;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// The forward discrete cosine transform for 8x8 blocks.
/// </summary>
/// <remarks>
/// <para>This is the plain definition of the transform, without the usual speed tricks.
/// The cosine values are written out as constants, so the result is the same on every platform:
/// the eight constants are cos(k * pi / 16) for k = 0 to 7.</para>
/// </remarks>
internal static class JpegForwardDct
{
    private const int Size = 8;

    private const double C0 = 1.0;
    private const double C1 = 0.98078528040323044;
    private const double C2 = 0.92387953251128676;
    private const double C3 = 0.83146961230254524;
    private const double C4 = 0.70710678118654752;
    private const double C5 = 0.55557023301960222;
    private const double C6 = 0.38268343236508977;
    private const double C7 = 0.19509032201612827;

    private static readonly double[] Cosines = BuildCosines();

    /// <summary>
    /// Transforms an 8x8 block (64 values, row by row) in place.
    /// </summary>
    public static void Transform(Span<double> block)
    {
        Span<double> line = stackalloc double[Size];
        Span<double> result = stackalloc double[Size];

        // rows
        for (var y = 0; y < Size; y++)
        {
            var row = block.Slice(y * Size, Size);
            Transform1D(row, result);
            result.CopyTo(row);
        }

        // columns
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                line[y] = block[(y * Size) + x];
            }

            Transform1D(line, result);

            for (var y = 0; y < Size; y++)
            {
                block[(y * Size) + x] = result[y];
            }
        }
    }

    private static void Transform1D(ReadOnlySpan<double> input, Span<double> output)
    {
        for (var u = 0; u < Size; u++)
        {
            var sum = 0.0;

            for (var n = 0; n < Size; n++)
            {
                sum += input[n] * Cosines[(u * Size) + n];
            }

            // The factor for u = 0 is 1/sqrt(2), which is the same value as C4.
            var scale = u == 0 ? C4 : 1.0;
            output[u] = 0.5 * scale * sum;
        }
    }

    /// <summary>
    /// Builds the table cos((2n + 1) * u * pi / 16) for u and n from 0 to 7.
    /// </summary>
    private static double[] BuildCosines()
    {
        // cos(k * pi / 16) for k = 0 to 31, built from the eight constants by symmetry.
        ReadOnlySpan<double> firstQuarter = [C0, C1, C2, C3, C4, C5, C6, C7];
        const int fullCircle = 32;
        const int halfCircle = 16;
        const int quarterCircle = 8;

        var circle = new double[fullCircle];

        for (var k = 0; k < fullCircle; k++)
        {
            var folded = k % halfCircle;
            var magnitude = folded switch
            {
                < quarterCircle => firstQuarter[folded],
                quarterCircle => 0.0,
                _ => -firstQuarter[halfCircle - folded],
            };

            circle[k] = k < halfCircle ? magnitude : -magnitude;
        }

        var table = new double[Size * Size];

        for (var u = 0; u < Size; u++)
        {
            for (var n = 0; n < Size; n++)
            {
                table[(u * Size) + n] = circle[(((2 * n) + 1) * u) % fullCircle];
            }
        }

        return table;
    }
}
