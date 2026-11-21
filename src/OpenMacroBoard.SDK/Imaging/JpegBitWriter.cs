using System.IO;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// Writes the entropy coded data of a JPEG scan bit by bit.
/// </summary>
internal sealed class JpegBitWriter
{
    private const int BitsPerByte = 8;
    private const int MarkerPrefix = 0xFF;

    private readonly Stream output;
    private int bitBuffer;
    private int bitCount;

    public JpegBitWriter(Stream output)
    {
        this.output = output;
    }

    /// <summary>
    /// Writes the lowest <paramref name="length"/> bits of <paramref name="bits"/>, most significant bit first.
    /// </summary>
    /// <param name="bits">The bits to write.</param>
    /// <param name="length">Number of bits, at most 16.</param>
    public void Write(int bits, int length)
    {
        // Less than eight bits are waiting, so the buffer holds at most 23 bits here.
        bitBuffer = (bitBuffer << length) | (bits & ((1 << length) - 1));
        bitCount += length;

        while (bitCount >= BitsPerByte)
        {
            bitCount -= BitsPerByte;
            WriteByte((bitBuffer >> bitCount) & 0xFF);
        }

        // keep only the bits that are still waiting
        bitBuffer &= (1 << bitCount) - 1;
    }

    /// <summary>
    /// Fills the last byte with one-bits, as required at the end of a scan.
    /// </summary>
    public void Flush()
    {
        if (bitCount != 0)
        {
            var missing = BitsPerByte - bitCount;
            Write((1 << missing) - 1, missing);
        }
    }

    private void WriteByte(int value)
    {
        output.WriteByte((byte)value);

        if (value == MarkerPrefix)
        {
            // A zero byte tells the decoder that this 0xFF is data and not a marker.
            output.WriteByte(0);
        }
    }
}
