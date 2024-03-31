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

    public abstract class StreamSourceExceptionWithKey : StreamSourceException
    {
        public readonly string key;

        protected StreamSourceExceptionWithKey(
            string message,
            string key,
            Exception innerException = null
            )
            : base(string.Format(message, key), innerException)
        {
            this.key = key;
        }
    }

    public class StreamAccessException : StreamSourceExceptionWithKey
    {
        public StreamAccessException(
            string key,
            Exception innerException = null
            )
            : base("Could not access stream for key \"{0}\".", key, innerException)
        { }
    }

    public class KeyNotFoundException : StreamSourceExceptionWithKey
    {
        public KeyNotFoundException(
            string key,
            Exception innerException = null
            )
            : base("Key \"{0}\" does not exist.", key, innerException)
        { }
    }

    public class IllegalKeyException : StreamSourceExceptionWithKey
    {
        public IllegalKeyException(
            string key,
            Exception innerException = null
            )
            : base("Key \"{0}\" is illegal.", key, innerException)
        { }
    }
}
