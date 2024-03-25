using ChaosUtil.Platform.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChaosFramework.IO.Streams.Sources
{
    public class FileStreamSource : StreamSource
    {
        public readonly DirectoryInfo rootDir;

        public FileStreamSource(DirectoryInfo rootDir)
        {
            this.rootDir = rootDir;
            if (!rootDir.Exists)
                throw new DirectoryNotFoundException();
        }

        IEnumerable<string> StreamSource.EnumerateKeys(string glob)
            => from file in rootDir.EnumerateFiles("*", SearchOption.AllDirectories)
               let globRegex = new Regex(GlobRegex.ConvertGlobToRegex(glob), RegexOptions.IgnoreCase)
               let key = file.FullName.Remove(0, rootDir.FullName.Length)
               where globRegex.IsMatch(key)
               select key;

        bool StreamSource.ContainsKey(string key)
            => File.Exists($"{rootDir.FullName}\\{key}");

        Stream StreamSource.OpenRead(string key)
            => File.OpenRead($"{rootDir.FullName}\\{key}");
    }
}
