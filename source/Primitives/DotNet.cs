using System;
using System.IO;

namespace ChaosFramework.IO.Primitives
{
    static class DotNet
    {
        public static void RegisterIO()
        {
            ChaosIO.AddType(ReadDateTime, WriteDateTime);
            ChaosIO.AddType(ReadGuid, WriteGuid);
        }

        static DateTime ReadDateTime(BinaryReader reader) => DateTime.FromBinary(reader.ReadInt64());
        static Guid ReadGuid(BinaryReader reader) => new Guid(reader.ReadBytes(16));

        static void WriteDateTime(BinaryWriter writer, DateTime value) => writer.Write(value.ToBinary());
        static void WriteGuid(BinaryWriter writer, Guid value) => writer.Write(value.ToByteArray());
    }
}
