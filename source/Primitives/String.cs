using System.IO;

namespace ChaosFramework.IO.Primitives
{
    static class String
    {
        /* TODO: Eliminate this class entirely by
         *       - registering string in ChaosFramework.IO.Primitives.BuiltIn
         *       - supporting writing as nullable separately (and use only where needed)
         */

        public static void RegisterIO()
            => ChaosIO.AddType(ReadString, WriteString);

        static string ReadString(BinaryReader reader)
        {
            bool isNull = reader.ReadBoolean();
            return isNull ? null : reader.ReadString();
        }

        static void WriteString(BinaryWriter writer, string value)
        {
            bool isNull = value == null;
            writer.Write(isNull);
            if (!isNull)
                writer.Write(value);
        }
    }
}
