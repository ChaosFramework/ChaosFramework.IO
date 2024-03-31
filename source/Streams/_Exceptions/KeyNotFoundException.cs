using System;

namespace ChaosFramework.IO.Streams
{
    public class KeyNotFoundException : StreamSourceExceptionWithKey
    {
        public KeyNotFoundException(
            string key,
            Exception innerException = null
            )
            : base("Key \"{0}\" does not exist.", key, innerException)
        { }
    }
}
