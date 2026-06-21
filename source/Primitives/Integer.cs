using System.IO;

namespace ChaosFramework.IO.Primitives
{
    public static class Integer
    {
        public static short ReadInt16_BigEndian(BinaryReader rd)
            => (short)((rd.ReadByte() << 8) | rd.ReadByte());

        public static ushort ReadUInt16_BigEndian(BinaryReader rd)
            => (ushort)((rd.ReadByte() << 8) | rd.ReadByte());

        public static int ReadInt32_BigEndian(BinaryReader rd)
            => (rd.ReadByte() << 24) | (rd.ReadByte() << 16) | (rd.ReadByte() << 8) | rd.ReadByte();

        public static uint ReadUInt32_BigEndian(BinaryReader rd)
            => (uint)((rd.ReadByte() << 24) | (rd.ReadByte() << 16) | (rd.ReadByte() << 8) | rd.ReadByte());

        public static long ReadInt64_BigEndian(BinaryReader rd)
            => unchecked(((long)ReadUInt32_BigEndian(rd) << 32) | ReadUInt32_BigEndian(rd));

        public static ulong ReadUInt64_BigEndian(BinaryReader rd)
            => unchecked(((ulong)ReadUInt32_BigEndian(rd) << 32) | ReadUInt32_BigEndian(rd));

        public static void WriteBigEndian(Stream s, short v)
        {
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        public static void WriteBigEndian(Stream s, ushort v)
        {
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        public static void WriteBigEndian(Stream s, int v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        public static void WriteBigEndian(Stream s, uint v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        public static void WriteBigEndian(Stream s, long v)
        {
            Integer.WriteBigEndian(s, (uint)(v >> 32));
            Integer.WriteBigEndian(s, (uint)v);
        }

        public static void WriteBigEndian(Stream s, ulong v)
        {
            Integer.WriteBigEndian(s, (uint)(v >> 32));
            Integer.WriteBigEndian(s, (uint)v);
        }
    }
}
