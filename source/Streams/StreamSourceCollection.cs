using ChaosUtil.Primitives;
using System.Collections;
using System.IO;
using System.Linq;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Streams
{
    public class StreamSourceCollection : SysCol.IEnumerable<StreamSource>, StreamSource
    {
        static bool Alive(StreamSource source) => source.alive;

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

        bool StreamSource.alive => sources.All(Alive);

        /// <summary>
        ///     Returns a new <see cref="StreamSourceCollection"/> consisting of the underlying
        ///     <see cref="StreamSource">StreamSources</see> of this instance concatenated with
        ///     the provided <paramref name="additionalSources">StreamSources</paramref>.
        /// </summary>
        /// <param name="additionalSources"> The sources to append. </param>
        public StreamSourceCollection Extend(SysCol.IEnumerable<StreamSource> additionalSources)
            => Extend(additionalSources.ToArray());

        /// <summary>
        ///     Returns a new <see cref="StreamSourceCollection"/> consisting of the underlying
        ///     <see cref="StreamSource">StreamSources</see> of this instance concatenated with
        ///     the provided <paramref name="additionalSources">StreamSources</paramref>.
        /// </summary>
        /// <param name="additionalSources"> The sources to append. </param>
        public StreamSourceCollection Extend(params StreamSource[] additionalSources)
        {
            StreamSource[] concat = new StreamSource[sources.Length + additionalSources.Length];
            sources.CopyTo(concat, 0);
            additionalSources.CopyTo(concat, sources.Length);
            return new StreamSourceCollection(concat);
        }
    }
}
