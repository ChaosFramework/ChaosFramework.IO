using System;
using System.IO;

namespace ChaosFramework.IO
{
    public static class BinaryReaderExtensions
    {
        public static T Read<T>(this BinaryReader reader)
            => (T)Read(reader, typeof(T));

        public static object Read(this BinaryReader reader, Type type)
        {
            if (type.IsArray)
                return Primitives.Array.Read(reader, type);
            else if (type.IsEnum)
                return Read(reader, type.GetEnumUnderlyingType());
            else
            {
                ChaosIO.Reader rd = ChaosIO.GetReader(type);
                if (rd != null)
                    return rd(reader);
                else
                    throw new Exception($"{typeof(ChaosIO)} does not define a reader for {type}.");
            }
        }

        public static T ReadSafe<T>(this BinaryReader reader, T defaultValue = default(T))
            => (T)ReadSafe(reader, typeof(T), defaultValue);

        public static object ReadSafe(this BinaryReader rd, Type type, object defaultValue = null)
        {
            TypedBinaryReader typedReader = rd as TypedBinaryReader;
            bool isSequentialTypedReader = typedReader != null && typedReader.mappingMethod == TypeMappingMethod.Sequential;

            int backupTypeCount = 0, backupMethodCount = 0;
            if (isSequentialTypedReader)
            {
                backupTypeCount = typedReader.typeMap.readTypes.Length;
                backupMethodCount = typedReader.typeMap.readMethods.Length;
            }
            object obj;
            long pos = rd.Read<long>();
            try { obj = Read(rd, type); }
            catch
            {
                if (isSequentialTypedReader)
                {
                    Array.Resize(ref typedReader.typeMap.readTypes, backupTypeCount);
                    Array.Resize(ref typedReader.typeMap.readMethods, backupMethodCount);
                }
                obj = defaultValue;
            }
            rd.BaseStream.Position = pos;
            if (isSequentialTypedReader)
            {
                pos = rd.Read<long>();
                try { typedReader.typeMap.Read(typedReader); }
                catch { rd.BaseStream.Position = pos; }
            }
            return obj;
        }
    }
}
