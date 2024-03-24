using ChaosFramework.Collections;
using ChaosUtil.Platform.Paths;
using System;
using System.IO;
using FilePosition = System.Tuple<string, long>;
using Linq = System.Linq.Enumerable;
using RelativeFullPathPair = System.Tuple<string, string>;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO
{
    public partial class ChaosArchive
    {
        public static partial class Tools
        {
            static readonly char[] lineSeparators = new char[] { '/', '\\' };

            static SysCol.IEnumerable<RelativeFullPathPair> EnumerateArchiveFiles(SysCol.IEnumerable<string> files, string baseDir)
            {
                foreach (string file in files ?? Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
                {
                    string normalizedFullPath = Normalization.NormalizeFullPath(file);
                    if (!normalizedFullPath.StartsWith(baseDir))
                        throw new Exception($"ChaosArchive - Invalid path {file}");

                    string relative = normalizedFullPath.Substring(baseDir.Length).TrimStart(lineSeparators);
                    yield return new RelativeFullPathPair(relative, normalizedFullPath);
                }
            }

            static string SelectRealtivePath(RelativeFullPathPair pair) => pair.Item1;

            public static void CreateArchive(string baseDirectory, SysCol.IEnumerable<string> files, string targetFile)
            {
                baseDirectory = Normalization.NormalizeFullPath(baseDirectory);
                using (ArchiveHash hash = new ArchiveHash())
                {
                    LinkedList<FilePosition> filePos = new LinkedList<FilePosition>();
                    using (FileStream archiveStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter archiveWriter = new BinaryWriter(archiveStream))
                    {
                        int numFiles = 0;
                        archiveWriter.WriteAs(0);
                        foreach (RelativeFullPathPair file in Linq.OrderBy(EnumerateArchiveFiles(files, baseDirectory), SelectRealtivePath))
                        {
                            // TODO: use WriteAs<string> as soon as it does not automatically write nullable
                            archiveWriter.Write(file.Item1);
                            filePos.Add(new FilePosition(file.Item2, archiveStream.Position));
                            archiveWriter.WriteAs(0L);
                            numFiles++;
                            hash.Transform(file.Item1);
                        }

                        int i = 0;
                        foreach (FilePosition file in filePos)
                        {
                            long currentFilePos = archiveStream.Position;
                            archiveStream.Position = file.Item2;
                            archiveWriter.WriteAs(currentFilePos);
                            archiveStream.Position = currentFilePos;
                            byte[] fileBytes = File.ReadAllBytes(file.Item1);
                            if (++i < filePos.length)
                                hash.Transform(fileBytes);
                            else
                                hash.TransformFinal(fileBytes);
                            archiveWriter.Write(fileBytes);
                        }

                        hash.Write(archiveWriter);
                        archiveStream.Position = 0;
                        archiveWriter.WriteAs(numFiles);
                    }
                }
            }
        }
    }
}
