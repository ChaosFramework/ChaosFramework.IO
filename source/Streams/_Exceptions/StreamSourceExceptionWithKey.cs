using System;

namespace ChaosFramework.IO.Streams
{
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
}
