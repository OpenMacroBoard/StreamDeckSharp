using System.Globalization;
using System.IO;
using System.Linq;

namespace StreamDeckSharp.Tests
{
    internal static class TextWriterExtensions
    {
        private static readonly (char, char)[] Lookup =
            Enumerable
                .Range(0, 256)
                .Select(b => ((byte)b).ToString("X2", CultureInfo.InvariantCulture))
                .Select(s => (s[0], s[1]))
                .ToArray();

        public static T WriteBinaryBlock<T>(
            this TextWriter writer,
            string blockName,
            T returnValue,
            byte[] data,
            int bytesPerLine = 16
        )
        {
            writer.WriteLine(blockName);
            writer.WriteLine("{");
            writer.WriteBinaryAsHex(data, "  ", bytesPerLine);
            writer.WriteLine();
            writer.WriteLine($"}} => return {returnValue}");
            writer.WriteLine();

            return returnValue;
        }

        public static void WriteBinaryAsHex(this TextWriter writer, byte[] data, string indentation = "", int bytesPerLine = 16)
        {
            if (data.Length == 0)
            {
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (i != 0)
                {
                    if (i % bytesPerLine == 0)
                    {
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.Write(' ');
                    }
                }

                if (i % bytesPerLine == 0)
                {
                    writer.Write(indentation);
                }

                if (data[i] != 0)
                {
                    writer.Write('0');
                    writer.Write('x');
                    writer.Write(Lookup[data[i]].Item1);
                    writer.Write(Lookup[data[i]].Item2);
                }
                else
                {
                    writer.Write("____");
                }
            }
        }
    }
}
