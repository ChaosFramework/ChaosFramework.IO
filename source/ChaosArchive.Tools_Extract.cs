using ChaosFramework.Collections;
using ChaosUtil.Platform.Paths;
using System.IO;
using System.Threading.Tasks;

namespace ChaosFramework.IO
{
    public partial class ChaosArchive
    {
        public static partial class Tools
        {
            class FileExtractionContext
            {
                public readonly ChaosArchive archive;
                public readonly string targetDirectory;
                public readonly string relative;

                public FileExtractionContext(ChaosArchive archive, string targetDirectory, string relative)
                {
                    this.archive = archive;
                    this.targetDirectory = targetDirectory;
                    this.relative = relative;
                }
            }

            public static void ExtractArchive(ChaosArchive archive, string targetDirectory, string glob = GlobRegex.MATCH_ALL_GLOB)
            {
                archive.AssertAlive();
                LinkedList<Task> tasks = new LinkedList<Task>();
                foreach (string file in archive.GetFiles(glob))
                {
                    Task t = new Task(ExtractFile, new FileExtractionContext(archive, targetDirectory, file));
                    tasks.Add(t);
                    t.Start();
                }
                Task.WaitAll(tasks.ToArray());
            }

            static void ExtractFile(object fileExtractionContext)
            {
                FileExtractionContext context = (FileExtractionContext)fileExtractionContext;
                FileInfo fileInfo = new FileInfo($"{context.targetDirectory}\\{context.relative}");
                fileInfo.Directory.Create();
                File.WriteAllBytes(fileInfo.FullName, context.archive.LoadFile(context.relative));
            }
        }
    }
}
