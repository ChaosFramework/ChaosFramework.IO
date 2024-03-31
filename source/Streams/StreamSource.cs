using System.IO;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public interface StreamSource
    {
        bool ContainsKey(string key);

        Stream OpenRead(string key);

        SysCol.IEnumerable<string> EnumerateKeys();

        /// <summary>
        ///     Returns whether this <see cref="StreamSource"/> is still alive.
        ///     If it is not, the behavior of any other call on this <see cref="StreamSource"/> is undefined.
        /// </summary>
        bool alive { get; }
    }
}
