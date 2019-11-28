using System;

namespace StreamDeckSharp.Exceptions
{
    /// <summary>
    /// Is thrown if no device could be found
    /// </summary>
    [Serializable]
    public class StreamDeckNotFoundException
        : StreamDeckException
    {
        public StreamDeckNotFoundException(string message)
            : base(message)
        {
        }

        public StreamDeckNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal StreamDeckNotFoundException()
            : base("Stream Deck not found.")
        {
        }
    }
}
