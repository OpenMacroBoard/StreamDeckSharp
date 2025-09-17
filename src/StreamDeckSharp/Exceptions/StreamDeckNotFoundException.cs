using System;
using System.Diagnostics.CodeAnalysis;

namespace StreamDeckSharp.Exceptions
{
    /// <summary>
    /// Is thrown if no device could be found
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class StreamDeckNotFoundException
        : StreamDeckException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckNotFoundException"/> class.
        /// </summary>
        internal StreamDeckNotFoundException()
            : base("Stream Deck not found.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        internal StreamDeckNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference
        /// if no inner exception is specified.
        /// </param>
        internal StreamDeckNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
