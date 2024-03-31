using System;

namespace ChaosFramework.IO.Streams
{
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
