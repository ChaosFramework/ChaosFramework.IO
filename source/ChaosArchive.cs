using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.IO.Streams;
using ChaosUtil.Platform.Paths;
using ChaosUtil.Primitives;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Linq = System.Linq.Enumerable;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO
{
    public partial class ChaosArchive : Disposable, StreamSource
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

        readonly SysCol.Dictionary<string, FilePos> filePos = new SysCol.Dictionary<string, FilePos>();
        readonly string[] directories;
        readonly System.IO.MemoryMappedFiles.MemoryMappedFile memFile;

        readonly SysCol.Dictionary<string, LinkedList<string>> cachedFileSearches = new SysCol.Dictionary<string, LinkedList<string>>();

        public ChaosArchive(FileInfo archiveFile, bool verifyChecksum)
        {
            this.archiveFile = archiveFile;
            if (!archiveFile.Exists)
                throw new FileNotFoundException("Archive file not found.", archiveFile.FullName);

            string lastFile = null;
            long nextFilePos = 0, lastFilePos = 0;
            FileStream str = File.OpenRead(archiveFile.FullName); // TODO: do we need to dispose this?
            BinaryReader rd = new BinaryReader(str);
            LinkedList<string> directories = new LinkedList<string>();

            int numFiles = rd.Read<int>();
            for (int i = 0; i < numFiles; i++)
            {
                string newFile = rd.ReadString();
                string directory = Normalization.NormalizeRelative(Path.GetDirectoryName(newFile)).TrimStart('\\', '/');

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
#if !NET8_0_OR_GREATER && !NETSTANDARD2_0_OR_GREATER
                    null,
#endif
                    HandleInheritability.None,
                    false
                    );
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create archive.", ex);
            }
        }

        public LinkedList<string> GetFilesCached(string glob = GlobRegex.MATCH_ALL_GLOB)
        {
            LinkedList<string> files;
            if (!cachedFileSearches.TryGetValue(glob, out files))
                cachedFileSearches[glob] = files = GetFiles(glob);
            return files;
        }

        public LinkedList<string> GetFiles(string glob = GlobRegex.MATCH_ALL_GLOB)
        {
            AssertAlive();
            Regex regex = new Regex(GlobRegex.ConvertGlobToRegex(glob), RegexOptions.Compiled | RegexOptions.IgnoreCase);

            LinkedList<string> files = new LinkedList<string>();
            foreach (string file in filePos.Keys)
                if (regex.IsMatch(file))
                    files.AddUnique(file);

            return files;
        }

        public LinkedList<string> GetFiles(string[] fileExtensions, string glob = GlobRegex.MATCH_ALL_GLOB)
        {
            AssertAlive();
            for (int i = 0; i < fileExtensions.Length; i++)
                fileExtensions[i] = fileExtensions[i].ToLower();

            LinkedList<string> files = new LinkedList<string>();
            foreach (string file in GetFiles(glob))
                foreach (string ext in fileExtensions)
                    if (file.EndsWith(ext))
                    {
                        files.AddUnique(file);
                        break;
                    }

            return files;
        }

        public SysCol.IEnumerable<string> EnumerateFiles()
        {
            AssertAlive();
            foreach (string file in filePos.Keys)
                yield return file;
        }

        public SysCol.IEnumerable<string> EnumerateFiles(
            string[] fileExtensions,
            string glob = GlobRegex.MATCH_ALL_GLOB
            )
        {
            AssertAlive();
            for (int i = 0; i < fileExtensions.Length; i++)
                fileExtensions[i] = fileExtensions[i].ToLower();

            foreach (string file in this.EnumerateKeys(glob))
                foreach (string ext in fileExtensions)
                    if (file.EndsWith(ext))
                        yield return file;
        }

        public LinkedList<string> GetDirectories(string baseDir)
        {
            AssertAlive();
            LinkedList<string> lst = new LinkedList<string>();
            string dir = Normalization.NormalizeRelative(baseDir);
            foreach (string str in directories)
                if (str != dir && str.StartsWith(dir))
                    lst.AddUnique(str);

            return lst;
        }

        public bool ContainsFile(string file)
        {
            AssertAlive();
            file = Normalization.NormalizeRelative(file);
            return filePos.ContainsKey(file);
        }

        public byte[] LoadFile(string file)
        {
            AssertAlive();
            file = Normalization.NormalizeRelative(file);

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

        public Stream OpenRead(string file)
        {
            AssertAlive();
            file = Normalization.NormalizeRelative(file);

            FilePos filePosition;
            if (!filePos.TryGetValue(file, out filePosition))
                throw new FileNotFoundException($"Archive does not contain file {file}.");

            return CreateStream(filePosition);
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

        bool StreamSource.ContainsKey(string key) => ContainsFile(key);
        SysCol.IEnumerable<string> StreamSource.EnumerateKeys() => EnumerateFiles();
        Stream StreamSource.OpenRead(string key) => OpenRead(key);

        protected override void DoDispose()
        {
            filePos.Clear();
            memFile.Dispose();
            base.DoDispose();
        }
    }
}
