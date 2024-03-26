using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChaosFramework.IO.Streams.Sources
{
    public class FileStreamSource : StreamSource
    {
        public readonly DirectoryInfo rootDir;

        public FileStreamSource(DirectoryInfo rootDir)
        {
            this.rootDir = rootDir;
        }

        public virtual IEnumerable<string> EnumerateKeys()
            => rootDir.Exists
             ? from file in rootDir.EnumerateFiles("*", SearchOption.AllDirectories)
               select file.FullName.Remove(0, rootDir.FullName.Length)
             : ChaosUtil.Primitives.Array<string>.empty;

        public virtual bool ContainsKey(string key)
            => File.Exists($"{rootDir.FullName}\\{key}");

        Stream StreamSource.OpenRead(string key)
            => File.OpenRead($"{rootDir.FullName}\\{key}");
    }
}
