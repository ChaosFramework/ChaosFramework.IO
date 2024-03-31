using System;

namespace ChaosFramework.IO.Streams
{
    public class StreamSourceException : Exception
    {
        public StreamSourceException(
            string message,
            Exception innerException = null
            )
            : base(message, innerException)
        { }
    }
}
