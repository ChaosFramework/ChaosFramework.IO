using ChaosUtil.Platform.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChaosFramework.IO.Streams.Sources
{
    public class CachedFileStreamSource : FileStreamSource
    {
        readonly HashSet<string> keys = new HashSet<string>();

        public CachedFileStreamSource(DirectoryInfo rootDir)
            : base(rootDir)
        {
            if (rootDir == null)
                throw new System.ArgumentNullException(nameof(rootDir));

            Refresh();
        }

        public override IEnumerable<string> EnumerateKeys()
            => from key in keys select key;

        public override bool ContainsKey(string key)
            => keys.Contains(Normalization.NormalizeRelative(key));

        public void Refresh()
        {
            keys.Clear();
            try
            {
                rootDir.Refresh();
                AssertReadPermission();
            }
            catch (System.Exception ex)
            {
                throw new StreamSourceException("Could not refresh directory!", ex);
            }

            foreach (string file in base.EnumerateKeys().Select(Normalization.NormalizeRelative))
                keys.Add(file);
        }
    }
}
