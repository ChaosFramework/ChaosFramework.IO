using ChaosUtil.Primitives;
using System.Collections;
using System.IO;
using System.Linq;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public class StreamSourceCollection : SysCol.IEnumerable<StreamSource>, StreamSource
    {
        readonly StreamSource[] sources;

        public StreamSourceCollection(params StreamSource[] sources)
        {
            this.sources = sources ?? Array<StreamSource>.empty;
        }

        bool StreamSource.ContainsKey(string key)
            => sources.Any(source => source.ContainsKey(key));

        Stream StreamSource.OpenRead(string key)
        {
            foreach (StreamSource source in sources)
            {
                Stream str;
                if (source.OpenReadIfExisting(key, out str))
                    return str;
            }

            throw new KeyNotFoundException(key, null);
        }

        SysCol.IEnumerable<string> StreamSource.EnumerateKeys()
        {
            SysCol.HashSet<string> keys = new SysCol.HashSet<string>();
            foreach (StreamSource source in sources)
                foreach (string key in source.EnumerateKeys())
                    if (keys.Add(key))
                        yield return key;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => ((SysCol.IEnumerable<StreamSource>)this).GetEnumerator();

        SysCol.IEnumerator<StreamSource> SysCol.IEnumerable<StreamSource>.GetEnumerator()
        {
            foreach (StreamSource source in sources)
                yield return source;
        }
    }
}
