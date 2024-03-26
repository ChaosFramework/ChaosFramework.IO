using ChaosUtil.Platform.Paths;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public static class StreamSourceExtensions
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

        public static SysCol.IEnumerable<string> EnumerateKeys(this StreamSource streamSource, string glob)
            => streamSource.EnumerateKeys(new Regex(GlobRegex.ConvertGlobToRegex(glob), RegexOptions.IgnoreCase));

        public static SysCol.IEnumerable<string> EnumerateKeys(this StreamSource streamSource, Regex regex)
            => from key in streamSource.EnumerateKeys()
               where regex.IsMatch(key)
               select key;
    }
}
