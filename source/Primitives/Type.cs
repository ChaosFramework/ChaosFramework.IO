using System;
using System.IO;

namespace ChaosFramework.IO.Primitives
{
    static class Type
    {
        public static void RegisterIO()
            => ChaosIO.AddType(Read, Write);

        static System.Type Read(BinaryReader rd)
        {
            TypedBinaryReader reader = rd as TypedBinaryReader;
            if (reader != null)
                return reader.ReadType();
            else
                throw new Exception($"Reading types is only supported with {nameof(TypedBinaryReader)}s.");
        }

        static void Write(BinaryWriter wr, System.Type type)
        {
            TypedBinaryWriter writer = wr as TypedBinaryWriter;
            if (writer != null)
                writer.Write(type);
            else
                throw new Exception($"Writing types is only supported with {nameof(TypedBinaryWriter)}s.");
        }
    }
}
