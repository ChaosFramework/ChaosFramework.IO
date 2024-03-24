using ChaosUtil.Reflection;
using System;
using System.IO;

namespace ChaosFramework.IO
{
    // TODO: consider reading/writing primitives using the BinaryReader/Writer functions directly (at least in framework libraries)

    public static class BinaryWriterExtensions
    {
        public static void WriteAs<ExplicitType>(this BinaryWriter writer, object obj)
            => writer.WriteAsUnchecked(typeof(ExplicitType), CastOrThrow(typeof(ExplicitType), obj));

        public static void WriteAs<ExplicitType>(this BinaryWriter writer, ExplicitType obj)
            => writer.WriteAsUnchecked(typeof(ExplicitType), obj);

        public static void WriteAs(this BinaryWriter writer, Type explicitType, object obj)
            => WriteAsUnchecked(writer, explicitType, CastOrThrow(explicitType, obj));

        static object CastOrThrow(Type explicitType, object obj)
        {
            if (obj != null && !Cast.TryCast(explicitType, obj, out obj))
                throw new Exception($"{obj.GetType()} can not be casted to {explicitType.FullName}.");

            return obj;
        }

        static void WriteAsUnchecked(this BinaryWriter writer, Type explicitType, object obj)
        {
            if (explicitType.IsArray)
                Primitives.Array.Write(writer, (Array)obj);
            else if (explicitType.IsEnum)
                WriteAs(writer, explicitType.GetEnumUnderlyingType(), Cast.Generic.Cast(explicitType.GetEnumUnderlyingType(), obj));
            else
            {
                ChaosIO.Writer wr = ChaosIO.GetWriter(explicitType);
                if (wr != null)
                    wr(writer, obj);
                else
                    throw new Exception($"{typeof(ChaosIO)} does not define a writer for {explicitType.FullName}.");
            }
        }

        public static void WriteSafe<ExplicitType>(this BinaryWriter writer, object obj)
            => writer.WriteAsSafeUnchecked(typeof(ExplicitType), CastOrThrow(typeof(ExplicitType), obj));

        public static void WriteSafe<ExplicitType>(this BinaryWriter writer, ExplicitType obj)
            => writer.WriteAsSafeUnchecked(typeof(ExplicitType), obj);

        static void WriteAsSafe(this BinaryWriter wr, Type explicitType, object obj)
            => WriteAsSafeUnchecked(wr, explicitType, CastOrThrow(explicitType, obj));

        static void WriteAsSafeUnchecked(this BinaryWriter wr, Type explicitType, object obj)
        {
            TypedBinaryWriter typedWriter = wr as TypedBinaryWriter;
            bool isSequentialTypedWriter = typedWriter != null && typedWriter.mappingMethod == TypeMappingMethod.Sequential;
            if (isSequentialTypedWriter)
                typedWriter.typeMap.SaveDelta(new TypedBinaryWriter(new MemoryStream(), TypeMappingMethod.DeltaStore));

            long pos = wr.BaseStream.Position;
            wr.WriteAs<long>(0);
            wr.WriteAsUnchecked(explicitType, obj);
            long pos2 = wr.BaseStream.Position;
            wr.BaseStream.Position = pos;
            wr.WriteAs<long>(pos2);
            wr.BaseStream.Position = pos2;

            if (isSequentialTypedWriter)
            {
                pos = wr.BaseStream.Position;
                wr.WriteAs<long>(0);
                typedWriter.typeMap.SaveDelta(typedWriter);
                pos2 = wr.BaseStream.Position;
                wr.BaseStream.Position = pos;
                wr.WriteAs<long>(pos2);
                wr.BaseStream.Position = pos2;
            }
        }
    }
}
