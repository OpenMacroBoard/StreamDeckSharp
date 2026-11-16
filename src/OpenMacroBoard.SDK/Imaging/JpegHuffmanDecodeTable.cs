using System;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// A Huffman table for decoding, as defined in ITU-T T.81, Annex F.
/// </summary>
internal sealed class JpegHuffmanDecodeTable
{
    private const int MaxCodeLength = 16;

    private readonly int[] minCode = new int[MaxCodeLength + 1];
    private readonly int[] maxCode = new int[MaxCodeLength + 1];
    private readonly int[] firstSymbol = new int[MaxCodeLength + 1];
    private readonly byte[] symbols;

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegHuffmanDecodeTable"/> class.
    /// </summary>
    /// <param name="codeCounts">Number of codes per code length (1 to 16 bits).</param>
    /// <param name="symbols">The symbols, ordered by code.</param>
    public JpegHuffmanDecodeTable(ReadOnlySpan<byte> codeCounts, ReadOnlySpan<byte> symbols)
    {
        this.symbols = symbols.ToArray();

        var code = 0;
        var symbolIndex = 0;

        for (var length = 1; length <= MaxCodeLength; length++)
        {
            int count = codeCounts[length - 1];

            if (count == 0)
            {
                maxCode[length] = -1;
            }
            else
            {
                firstSymbol[length] = symbolIndex;
                minCode[length] = code;
                code += count;
                symbolIndex += count;
                maxCode[length] = code - 1;
            }

            code <<= 1;
        }

        if (symbolIndex != symbols.Length)
        {
            throw new ImageDecodingException("A Huffman table of the JPEG file is invalid.");
        }
    }

    /// <summary>
    /// Finds the symbol for a code, or returns -1 if there is no code with that length and value.
    /// </summary>
    public int Find(int code, int length)
    {
        if (maxCode[length] < 0 || code > maxCode[length] || code < minCode[length])
        {
            return -1;
        }

        return symbols[firstSymbol[length] + code - minCode[length]];
    }
}
