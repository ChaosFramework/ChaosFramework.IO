using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosUtil.Platform.Paths;
using ChaosUtil.Primitives;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Linq = System.Linq.Enumerable;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO
{
    public partial class ChaosArchive : Disposable
    {
        struct FilePos
        {
            public readonly long position;
            public readonly int length;

            public FilePos(long position, int length)
            {
                this.position = position;
                this.length = length;
            }
        }

        static string GetMemFileName(FileInfo archiveFile)
            => "__ChaosArchive__"
               + System.Diagnostics.Process.GetCurrentProcess().Id + "__"
               + EliminateIllegalMemFileCharacters(ChaosUtil.Reflection.AssemblyMeta.productName) + "__"
               + EliminateIllegalMemFileCharacters(archiveFile.FullName);

        static string EliminateIllegalMemFileCharacters(string str)
            => str.Replace(" ", "__")
                  .Replace("\\", "__")
                  .Replace("/", "__")
                  .Replace(":", "__");

        public readonly FileInfo archiveFile;
        public readonly DirectoryInfo overrideDirectory;

        readonly SysCol.Dictionary<string, FilePos> filePos = new SysCol.Dictionary<string, FilePos>();
        readonly string[] directories;
        readonly System.IO.MemoryMappedFiles.MemoryMappedFile memFile;

        readonly SysCol.Dictionary<string, LinkedList<string>> cachedFileSearches = new SysCol.Dictionary<string, LinkedList<string>>();

        SysCol.HashSet<string> overrideFiles, overrideDirectories;

        public ChaosArchive(string archivePath, bool verifyChecksum)
        {
            archiveFile = new FileInfo(archivePath);
            if (!archiveFile.Exists)
                throw new FileNotFoundException("Archive file not found.", archivePath);

            string archiveName = Path.GetFileNameWithoutExtension(archiveFile.Name);
            overrideDirectory = new DirectoryInfo($"{archiveFile.Directory.FullName}\\{archiveName}");

            UpdateOverrideFiles();

            string lastFile = null;
            long nextFilePos = 0, lastFilePos = 0;
            FileStream str = File.OpenRead(archiveFile.FullName); // TODO: do we need to dispose this?
            BinaryReader rd = new BinaryReader(str);
            string baseDir = Normalization.NormalizeFullPath(null);
            LinkedList<string> directories = new LinkedList<string>();

            int numFiles = rd.Read<int>();
            for (int i = 0; i < numFiles; i++)
            {
                string newFile = rd.ReadString();
                string directory = Normalization.NormalizeFullPath(Path.GetDirectoryName(newFile))
                                                .Remove(0, baseDir.Length)
                                                .TrimStart('\\');

                directories.AddUnique(directory);
                nextFilePos = rd.Read<long>();
                if (lastFile != null)
                    filePos[lastFile] = new FilePos(lastFilePos, (int)(nextFilePos - lastFilePos));

                lastFile = newFile;
                lastFilePos = nextFilePos;
            }

            if (lastFile != null)
                filePos[lastFile] = new FilePos(lastFilePos, (int)(rd.BaseStream.Length - ArchiveHash.NUM_HASH_BYTES - lastFilePos));

            if (verifyChecksum)
                using (ArchiveHash builtHash = ArchiveHash.GetHash(filePos.Keys, file =>
                {
                    FilePos pos = filePos[file];
                    long oldPos = str.Position;
                    str.Position = pos.position;
                    byte[] buffer = rd.ReadBytes(pos.length);
                    str.Position = oldPos;
                    return buffer;
                }))
                {
                    rd.BaseStream.Position = rd.BaseStream.Length - ArchiveHash.NUM_HASH_BYTES;
                    byte[] readHash = rd.ReadBytes(ArchiveHash.NUM_HASH_BYTES);
                    if (!Linq.SequenceEqual(readHash, builtHash.bytes))
                        throw new InvalidDataException("Archive is corrupted.");
                }

            this.directories = directories.ToArray();
            try
            {
                memFile = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(
                    str,
                    GetMemFileName(archiveFile),
                    0,
                    System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read,
                    null,
                    HandleInheritability.None,
                    false
                    );
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create archive.", ex);
            }
        }

        public bool ContainsOverrideFile(string path) => overrideFiles.Contains(path);

        public void UpdateOverrideFiles()
        {
            AssertAlive();
            cachedFileSearches.Clear();
            string[] files = overrideDirectory.Exists
                ? Directory.GetFiles(overrideDirectory.FullName, "*", SearchOption.AllDirectories)
                : Array<string>.empty;
            string[] directories = overrideDirectory.Exists
                ? Directory.GetDirectories(overrideDirectory.FullName, "*", SearchOption.AllDirectories)
                : Array<string>.empty;

            overrideFiles = new SysCol.HashSet<string>();
            overrideDirectories = new SysCol.HashSet<string>();

            for (int i = 0; i < files.Length; i++)
                overrideFiles.Add(files[i].Substring(overrideDirectory.FullName.Length + 1).ToLower());
            for (int i = 0; i < directories.Length; i++)
                overrideDirectories.Add(directories[i].Substring(overrideDirectory.FullName.Length + 1).ToLower());
        }

        public LinkedList<string> GetFilesCached(string filter = GlobRegex.MATCH_ALL_GLOB)
        {
            LinkedList<string> files;
            if (!cachedFileSearches.TryGetValue(filter, out files))
                cachedFileSearches[filter] = files = GetFiles(filter, true);
            return files;
        }

        public LinkedList<string> GetFiles(string filter = GlobRegex.MATCH_ALL_GLOB, bool allowOverride = true)
        {
            AssertAlive();
            Regex regex = new Regex(GlobRegex.ConvertGlobToRegex(filter), RegexOptions.Compiled | RegexOptions.IgnoreCase);

            LinkedList<string> files = new LinkedList<string>();
            if (allowOverride)
                foreach (string file in overrideFiles)
                {
                    string relative = Normalization.NormalizeRelative(file);
                    if (regex.IsMatch(relative))
                        files.Add(relative);
                }

            foreach (string file in filePos.Keys)
                if (regex.IsMatch(file))
                    files.AddUnique(file);

            return files;
        }

        public LinkedList<string> GetFiles(string[] fileExtensions, string filter = GlobRegex.MATCH_ALL_GLOB, bool allowOverride = true)
        {
            AssertAlive();
            for (int i = 0; i < fileExtensions.Length; i++)
                fileExtensions[i] = fileExtensions[i].ToLower();

            LinkedList<string> files = new LinkedList<string>();
            foreach (string file in GetFiles(filter, allowOverride))
                foreach (string ext in fileExtensions)
                    if (file.EndsWith(ext))
                    {
                        files.AddUnique(file);
                        break;
                    }

            return files;
        }

        public SysCol.IEnumerable<string> EnumerateFiles(string filter = GlobRegex.MATCH_ALL_GLOB, bool allowOverride = true)
        {
            AssertAlive();
            Regex regex = new Regex(GlobRegex.ConvertGlobToRegex(filter), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            SysCol.HashSet<string> files = new SysCol.HashSet<string>();
            if (allowOverride)
                foreach (string file in overrideFiles)
                {
                    string relative = Normalization.NormalizeRelative(file);
                    if (regex.IsMatch(relative))
                    {
                        files.Add(relative);
                        yield return relative;
                    }
                }

            foreach (string file in filePos.Keys)
                if (regex.IsMatch(file))
                    if (!files.Contains(file))
                        yield return file;
        }

        public SysCol.IEnumerable<string> EnumerateFiles(
            string[] fileExtensions,
            string filter = GlobRegex.MATCH_ALL_GLOB,
            bool allowOverride = true
            )
        {
            AssertAlive();
            for (int i = 0; i < fileExtensions.Length; i++)
                fileExtensions[i] = fileExtensions[i].ToLower();

            SysCol.HashSet<string> files = new SysCol.HashSet<string>();
            foreach (string file in EnumerateFiles(filter, allowOverride))
                foreach (string ext in fileExtensions)
                    if (file.EndsWith(ext))
                        if (files.Add(file))
                            yield return file;
        }

        public LinkedList<string> GetDirectories(string baseDir, bool allowOverride = true)
        {
            AssertAlive();
            LinkedList<string> lst = new LinkedList<string>();
            string dir = Normalization.NormalizeRelative(baseDir);
            foreach (string str in directories)
                if (str != dir && str.StartsWith(dir))
                    lst.AddUnique(str);

            if (allowOverride)
                foreach (string str in overrideDirectories)
                    if (str != dir && str.StartsWith(dir))
                        lst.AddUnique(str);

            return lst;
        }

        public bool ContainsFile(string file, bool allowOverride = true)
        {
            AssertAlive();
            file = Normalization.NormalizeRelative(file);
            return (allowOverride && overrideFiles.Contains(file)) || filePos.ContainsKey(file);
        }

        public byte[] LoadFile(string file, bool allowOverride = true)
        {
            AssertAlive();
            file = Normalization.NormalizeRelative(file);
            if (allowOverride && overrideFiles.Contains(file))
            {
                string ioFile = $"{overrideDirectory.FullName}\\{file}";
                return File.ReadAllBytes(ioFile);
            }
            else
            {
                FilePos filePosition;
                if (!filePos.TryGetValue(file, out filePosition))
                    throw new FileNotFoundException($"Archive does not contain file {file}.");

                if (filePosition.length == 0)
                    return Array<byte>.empty;

                using (Stream str = CreateStream(filePosition))
                {
                    byte[] output = new byte[filePosition.length];
                    str.Read(output, 0, filePosition.length);
                    return output;
                }
            }
        }

        public Stream OpenRead(string file) => OpenRead(file, true);

        public Stream OpenRead(string file, bool allowOverride)
        {
            AssertAlive();
            file = Normalization.NormalizeRelative(file);
            if (allowOverride && overrideFiles.Contains(file))
            {
                string ioFile = $"{overrideDirectory.FullName}\\{file}";
                return File.OpenRead(ioFile);
            }
            else
            {
                FilePos filePosition;
                if (!filePos.TryGetValue(file, out filePosition))
                    throw new FileNotFoundException($"Archive does not contain file {file}.");

                return CreateStream(filePosition);
            }
        }

        Stream CreateStream(FilePos filePosition)
        {
            if (filePosition.length == 0)
                return new MemoryStream(Array<byte>.empty);
            else
                lock (memFile)
                    return memFile.CreateViewStream(
                        filePosition.position,
                        filePosition.length,
                        System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read
                        );
        }

        protected override void DoDispose()
        {
            filePos.Clear();
            memFile.Dispose();
            base.DoDispose();
        }
    }
}
