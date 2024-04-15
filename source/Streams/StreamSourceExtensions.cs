using ChaosUtil.Platform.Paths;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public static class StreamSourceExtensions
    {
        /// <summary> Retrieve a stream for the given key, if it exists. </summary>
        /// <param name="streamSource"> The streamSource to retrieve the stream from. </param>
        /// <param name="key"> The key to retrieve the stream for. </param>
        /// <param name="stream"> The retrieved stream if successful; <see langword="null"/> otherwise. </param>
        /// <returns>
        ///     <see langword="true"/> if a stream was retrieved;
        ///     <see langword="false"/> if no stream was found.
        /// </returns>
        public static bool OpenReadIfExisting(this StreamSource streamSource, string key, out Stream stream)
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
