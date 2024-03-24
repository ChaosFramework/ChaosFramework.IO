using ChaosFramework.Core;
using System.IO;

namespace ChaosFramework.IO
{
    public class BufferedFileStream : Disposable
    {
        readonly FileStream actualStream;
        readonly string targetFile, bufferFile;

        public BufferedFileStream(string path, string bufferFilePath = null)
        {
            targetFile = path;
            bufferFile = bufferFilePath ?? $"{targetFile}.buffer";

            if (File.Exists(bufferFile))
            {
                int i = 2;
                string newBufferFile;
                while (File.Exists(newBufferFile = $"{bufferFile}({i})"))
                    i++;
                bufferFile = newBufferFile;
            }

            actualStream = File.OpenWrite(bufferFile);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            actualStream?.Dispose();
            new System.Threading.Tasks.Task(CopyBufferFile).Start();
        }

        void CopyBufferFile()
        {
        try_to_copy:
            try
            {
                if (File.Exists(targetFile))
                    File.Delete(targetFile);
                File.Move(bufferFile, targetFile);
            }
            catch
            {
                System.Threading.Thread.Sleep(1);
                goto try_to_copy;
            }
        }

        public static implicit operator Stream(BufferedFileStream str) => str.actualStream;
    }
}
