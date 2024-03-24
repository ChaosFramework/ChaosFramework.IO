using System.IO;

namespace ChaosFramework.IO.Primitives
{
    public static class Integer
    {
        public static short ReadInt16_BigEndian(BinaryReader rd)
        {
            byte[] buffer = rd.ReadBytes(2);
            System.Array.Reverse(buffer);
            return System.BitConverter.ToInt16(buffer, 0);
        }

        public static int ReadInt32_BigEndian(BinaryReader rd)
        {
            byte[] buffer = rd.ReadBytes(4);
            System.Array.Reverse(buffer);
            return System.BitConverter.ToInt32(buffer, 0);
        }

        public static long ReadInt64_BigEndian(BinaryReader rd)
        {
            byte[] buffer = rd.ReadBytes(8);
            System.Array.Reverse(buffer);
            return System.BitConverter.ToInt64(buffer, 0);
        }
    }
}
