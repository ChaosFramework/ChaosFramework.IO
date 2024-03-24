using System.IO;

namespace ChaosFramework.IO.Primitives
{
    static class BuiltIn
    {
        public static void RegisterIO()
        {
            ChaosIO.AddType(ReadSByte, WriteSByte);
            ChaosIO.AddType(ReadByte, WriteByte);
            ChaosIO.AddType(ReadShort, WriteShort);
            ChaosIO.AddType(ReadUShort, WriteUShort);
            ChaosIO.AddType(ReadInt, WriteInt);
            ChaosIO.AddType(ReadUInt, WriteUInt);
            ChaosIO.AddType(ReadLong, WriteLong);
            ChaosIO.AddType(ReadULong, WriteULong);
            ChaosIO.AddType(ReadFloat, WriteFloat);
            ChaosIO.AddType(ReadDouble, WriteDouble);
            ChaosIO.AddType(ReadDecimal, WriteDecimal);
            ChaosIO.AddType(ReadBool, WriteBool);
            ChaosIO.AddType(ReadChar, WriteChar);
        }

        static sbyte ReadSByte(BinaryReader reader) => reader.ReadSByte();
        static byte ReadByte(BinaryReader reader) => reader.ReadByte();
        static short ReadShort(BinaryReader reader) => reader.ReadInt16();
        static ushort ReadUShort(BinaryReader reader) => reader.ReadUInt16();
        static int ReadInt(BinaryReader reader) => reader.ReadInt32();
        static uint ReadUInt(BinaryReader reader) => reader.ReadUInt32();
        static long ReadLong(BinaryReader reader) => reader.ReadInt64();
        static ulong ReadULong(BinaryReader reader) => reader.ReadUInt64();
        static float ReadFloat(BinaryReader reader) => reader.ReadSingle();
        static double ReadDouble(BinaryReader reader) => reader.ReadDouble();
        static decimal ReadDecimal(BinaryReader reader) => reader.ReadDecimal();
        static bool ReadBool(BinaryReader reader) => reader.ReadBoolean();
        static char ReadChar(BinaryReader reader) => reader.ReadChar();

        static void WriteSByte(BinaryWriter writer, sbyte value) => writer.Write(value);
        static void WriteByte(BinaryWriter writer, byte value) => writer.Write(value);
        static void WriteShort(BinaryWriter writer, short value) => writer.Write(value);
        static void WriteUShort(BinaryWriter writer, ushort value) => writer.Write(value);
        static void WriteInt(BinaryWriter writer, int value) => writer.Write(value);
        static void WriteUInt(BinaryWriter writer, uint value) => writer.Write(value);
        static void WriteLong(BinaryWriter writer, long value) => writer.Write(value);
        static void WriteULong(BinaryWriter writer, ulong value) => writer.Write(value);
        static void WriteFloat(BinaryWriter writer, float value) => writer.Write(value);
        static void WriteDouble(BinaryWriter writer, double value) => writer.Write(value);
        static void WriteDecimal(BinaryWriter writer, decimal value) => writer.Write(value);
        static void WriteBool(BinaryWriter writer, bool value) => writer.Write(value);
        static void WriteChar(BinaryWriter writer, char value) => writer.Write(value);
    }
}
