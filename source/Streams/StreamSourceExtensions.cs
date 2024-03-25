using System.IO;

namespace ChaosFramework.IO.Streams
{
    static class StreamSourceExtensions
    {
        public static bool TryOpenRead(this StreamSource streamSource, string key, out Stream stream)
        {
            if (streamSource.ContainsKey(key))
            {
                stream = streamSource.OpenRead(key);
                return true;
            }

            stream = null;
            return false;
        }
    }
}
