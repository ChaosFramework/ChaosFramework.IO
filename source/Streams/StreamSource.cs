using System.IO;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public interface StreamSource
    {
        bool ContainsKey(string key);

        Stream OpenRead(string key);

        SysCol.IEnumerable<string> EnumerateKeys(string filter);
    }
}
