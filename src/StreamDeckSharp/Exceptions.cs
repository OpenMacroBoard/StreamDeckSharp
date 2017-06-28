using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckSharp.Exceptions
{
    /// <summary>
    /// Base class for all StreamDeckSharp Exceptions
    /// </summary>
    public abstract class StreamDeckException : Exception
    {
        public StreamDeckException(string Message) : base(Message) { }
    }

    public class StreamDeckNotFoundException : StreamDeckException
    {
        public StreamDeckNotFoundException() : base("Stream Deck not found.") { }
    }
}
