using System;
using System.Diagnostics.CodeAnalysis;

namespace StreamDeckSharp.Exceptions
{
    /// <summary>
    /// Base class for all StreamDeckSharp Exceptions
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public abstract class StreamDeckException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckException"/> class.
        /// </summary>
        protected StreamDeckException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected StreamDeckException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference
        /// if no inner exception is specified.
        /// </param>
        protected StreamDeckException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
