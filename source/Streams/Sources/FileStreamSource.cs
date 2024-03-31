using ChaosUtil.Platform.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileSystemRights = System.Security.AccessControl.FileSystemRights;

namespace ChaosFramework.IO.Streams.Sources
{
    public class FileStreamSource : StreamSource
    {
        public readonly DirectoryInfo rootDir;

        public FileStreamSource(DirectoryInfo rootDir)
        {
            this.rootDir = rootDir;
            AssertReadPermission();
        }

        protected void AssertReadPermission()
        {
            if (rootDir.Exists && !FileSystem.HasAccess(rootDir.FullName, FileSystemRights.Read))
                throw new StreamSourceException($"Could not access root directory \"{rootDir.Name}\"!");
        }

        public virtual IEnumerable<string> EnumerateKeys()
        {
            try
            {
                return rootDir.Exists
                     ? from file in rootDir.EnumerateFiles("*", SearchOption.AllDirectories)
                       select file.FullName.Remove(0, rootDir.FullName.Length)
                     : ChaosUtil.Primitives.Array<string>.empty;
            }
            catch (System.Security.SecurityException)
            {
                return ChaosUtil.Primitives.Array<string>.empty;
            }
        }

        public virtual bool ContainsKey(string key)
        {
            try
            {
                return File.Exists($"{rootDir.FullName}\\{key}");
            }
            catch
            {
                return false;
            }
        }

        Stream StreamSource.OpenRead(string key)
        {
            try { return File.OpenRead($"{rootDir.FullName}\\{key}"); }
            catch (PathTooLongException ex) { throw InvalidPathException(key, ex); }
            catch (System.ArgumentException ex) { throw InvalidPathException(key, ex); }
            catch (System.NotSupportedException ex) { throw InvalidPathException(key, ex); }
            catch (FileNotFoundException ex) { throw new KeyNotFoundException(key, ex); }
            catch (DirectoryNotFoundException ex) { throw new KeyNotFoundException(key, ex); }
            catch (System.UnauthorizedAccessException ex) { throw new StreamAccessException(key, ex); }
        }

        static System.Exception InvalidPathException(string key, System.Exception ex)
            => new IllegalKeyException(key, ex);
    }
}
