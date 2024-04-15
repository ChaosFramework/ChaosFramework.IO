using System;

namespace ChaosFramework.IO.Streams
{
    public class KeyFormatException : StreamSourceExceptionWithKey
    {
        public KeyFormatException(
            string key,
            Exception innerException = null
            )
            : base("The key \"{0}\" has an illegal format.", key, innerException)
        { }
    }
}
