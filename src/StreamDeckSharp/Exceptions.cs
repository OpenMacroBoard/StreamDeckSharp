using System;

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
