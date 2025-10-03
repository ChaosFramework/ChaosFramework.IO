using System;
using System.IO;

namespace ChaosFramework.IO.Streams
{
    public class StreamView : Stream
    {
        readonly Stream baseStream;
        readonly long offset;
        readonly long length;
        bool disposed;

        public StreamView(Stream baseStream, long offset, long length)
        {
            this.baseStream = baseStream;
            this.offset = offset;
            this.length = length;
        }

        public override bool CanRead => !disposed && baseStream.CanRead;

        public override bool CanSeek => !disposed && baseStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get { return Math.Max(0, Math.Min(length, baseStream.Position - offset)); }
            set { baseStream.Position = Math.Max(0, value) + offset; }
        }

        public override void Flush() => new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(StreamView));

            return baseStream.Read(buffer, offset, Math.Min(count, (int)(length - Position)));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(StreamView));

            switch (origin)
            {
                case SeekOrigin.Begin: return Position = offset;
                case SeekOrigin.Current: return Position += offset;
                case SeekOrigin.End: return Position = length - offset;
                default: throw new ArgumentOutOfRangeException(nameof(origin));
            }
        }

        public override void SetLength(long value) => new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            disposed = true;
        }
    }
}
