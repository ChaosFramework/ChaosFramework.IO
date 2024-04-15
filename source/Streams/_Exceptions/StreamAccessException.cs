using System;

namespace ChaosFramework.IO.Streams
{
    public class StreamAccessException : StreamSourceExceptionWithKey
    {
        public StreamAccessException(
            string key,
            Exception innerException = null
            )
            : base("Could not access stream for key \"{0}\".", key, innerException)
        { }
    }
}
