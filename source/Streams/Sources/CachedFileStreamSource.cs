using ChaosUtil.Platform.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChaosFramework.IO.Streams.Sources
{
    public class CachedFileStreamSource : FileStreamSource
    {
        readonly HashSet<string> keys = new HashSet<string>();

        public CachedFileStreamSource(DirectoryInfo rootDir)
            : base(rootDir)
        {
            Refresh();
        }

        public override IEnumerable<string> EnumerateKeys(string glob)
            => from key in keys
               let globRegex = new Regex(GlobRegex.ConvertGlobToRegex(glob), RegexOptions.IgnoreCase)
               where globRegex.IsMatch(key)
               select key;

        public override bool ContainsKey(string key)
            => keys.Contains(Normalization.NormalizeRelative(key));

        public void Refresh()
        {
            keys.Clear();
            foreach (string file in base.EnumerateKeys().Select(Normalization.NormalizeRelative))
                keys.Add(file);
        }
    }
}
