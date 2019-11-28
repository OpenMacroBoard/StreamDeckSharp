using System;

namespace StreamDeckSharp.Exceptions
{
    /// <summary>
    /// Base class for all StreamDeckSharp Exceptions
    /// </summary>
    [Serializable]
    public abstract class StreamDeckException : Exception
    {
        public StreamDeckException()
        {
        }

        public StreamDeckException(string message)
            : base(message)
        {
        }

        public StreamDeckException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
