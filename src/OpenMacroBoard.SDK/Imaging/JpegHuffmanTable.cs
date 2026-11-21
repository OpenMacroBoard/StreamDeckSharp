using System;

namespace OpenMacroBoard.SDK.Imaging;

/// <summary>
/// A Huffman table for encoding: the code and the code length for every symbol.
/// </summary>
internal sealed class JpegHuffmanTable
{
    private const int SymbolCount = 256;
    private const int MaxCodeLength = 16;

    private readonly ushort[] codes = new ushort[SymbolCount];
    private readonly byte[] lengths = new byte[SymbolCount];
    private readonly byte[] codeCounts;
    private readonly byte[] symbols;

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegHuffmanTable"/> class.
    /// </summary>
    /// <param name="codeCounts">Number of codes per code length (1 to 16 bits).</param>
    /// <param name="symbols">The symbols, ordered by code.</param>
    public JpegHuffmanTable(ReadOnlySpan<byte> codeCounts, ReadOnlySpan<byte> symbols)
    {
        if (codeCounts.Length != MaxCodeLength)
        {
            throw new ArgumentException("Exactly 16 code counts are expected.", nameof(codeCounts));
        }

        this.codeCounts = codeCounts.ToArray();
        this.symbols = symbols.ToArray();

        // Codes are assigned in ascending order, as defined in ITU-T T.81, Annex C.
        var code = 0;
        var symbolIndex = 0;

        for (var length = 1; length <= MaxCodeLength; length++)
        {
            for (var i = 0; i < codeCounts[length - 1]; i++)
            {
                var symbol = symbols[symbolIndex];
                codes[symbol] = (ushort)code;
                lengths[symbol] = (byte)length;

                symbolIndex++;
                code++;
            }

            code <<= 1;
        }

        if (symbolIndex != symbols.Length)
        {
            throw new ArgumentException("The number of symbols does not match the code counts.", nameof(symbols));
        }
    }

    /// <summary>
    /// Gets the number of codes per code length (1 to 16 bits).
    /// </summary>
    public ReadOnlySpan<byte> CodeCounts => codeCounts;

    /// <summary>
    /// Gets the symbols, ordered by code.
    /// </summary>
    public ReadOnlySpan<byte> Symbols => symbols;

    /// <summary>
    /// Gets the code of a symbol.
    /// </summary>
    public int GetCode(int symbol)
    {
        return codes[symbol];
    }

    /// <summary>
    /// Gets the code length of a symbol in bits.
    /// </summary>
    public int GetLength(int symbol)
    {
        return lengths[symbol];
    }
}
