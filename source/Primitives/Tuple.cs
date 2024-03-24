using ChaosFramework.Collections.Immutable;
using System;
using System.IO;
using System.Reflection;
using static ChaosFramework.IO.ChaosIO;
using ReaderWriterPair = System.Tuple<System.Reflection.MethodInfo, System.Reflection.MethodInfo>;

namespace ChaosFramework.IO.Primitives
{
    static class Tuple
    {
        const BindingFlags PRIVATE_STATIC = BindingFlags.NonPublic | BindingFlags.Static;

        static readonly ImmutableArray<ReaderWriterPair> readWriteMethods;

        static Tuple()
        {
            // TODO: Stop identifying Load/Write methods by name
            ReaderWriterPair[] readWriteMethods = new ReaderWriterPair[8];
            for (int i = 1; i <= 8; i++)
                readWriteMethods[i - 1] = new ReaderWriterPair(
                    typeof(Tuple).GetMethod("LoadTuple" + i, PRIVATE_STATIC),
                    typeof(Tuple).GetMethod("WriteTuple" + i, PRIVATE_STATIC)
                    );
            Tuple.readWriteMethods = readWriteMethods;
        }

        public static void RegisterIO()
        {
            genericReaders.Add(MakeGenericReader);
            genericWriters.Add(MakeGenericWriter);
        }

        static Reader MakeGenericReader(System.Type type)
        {
            if (!type.IsGenericType)
                return null;

            // TODO: Stop identifying Tuple<...> classes by name
            int numGenericArguments = type.GetGenericArguments().Length;
            if (numGenericArguments <= 8 && type.GetGenericTypeDefinition().FullName == "System.Tuple`" + numGenericArguments)
                return ChaosIO.MakeGenericReader(type, readWriteMethods[numGenericArguments - 1].Item1);

            return null;
        }

        static Writer MakeGenericWriter(System.Type type)
        {
            if (!type.IsGenericType)
                return null;

            // TODO: Stop identifying Tuple<...> classes by name
            int numGenericArguments = type.GetGenericArguments().Length;
            if (numGenericArguments <= 8 && type.GetGenericTypeDefinition().FullName == "System.Tuple`" + numGenericArguments)
                return ChaosIO.MakeGenericWriter(type, readWriteMethods[numGenericArguments - 1].Item2);

            return null;
        }

        static Tuple<T1> LoadTuple1<T1>(BinaryReader reader)
          => new Tuple<T1>(reader.Read<T1>());

        static Tuple<T1, T2> LoadTuple2<T1, T2>(BinaryReader reader)
          => new Tuple<T1, T2>(reader.Read<T1>(), reader.Read<T2>());

        static Tuple<T1, T2, T3> LoadTuple3<T1, T2, T3>(BinaryReader reader)
          => new Tuple<T1, T2, T3>(reader.Read<T1>(), reader.Read<T2>(), reader.Read<T3>());

        static Tuple<T1, T2, T3, T4> LoadTuple4<T1, T2, T3, T4>(BinaryReader reader)
          => new Tuple<T1, T2, T3, T4>(reader.Read<T1>(), reader.Read<T2>(), reader.Read<T3>(), reader.Read<T4>());

        static Tuple<T1, T2, T3, T4, T5> LoadTuple5<T1, T2, T3, T4, T5>(BinaryReader reader)
          => new Tuple<T1, T2, T3, T4, T5>(
              reader.Read<T1>(), reader.Read<T2>(), reader.Read<T3>(), reader.Read<T4>(),
              reader.Read<T5>()
              );

        static Tuple<T1, T2, T3, T4, T5, T6> LoadTuple6<T1, T2, T3, T4, T5, T6>(BinaryReader reader)
          => new Tuple<T1, T2, T3, T4, T5, T6>(
              reader.Read<T1>(), reader.Read<T2>(), reader.Read<T3>(), reader.Read<T4>(),
              reader.Read<T5>(), reader.Read<T6>()
              );

        static Tuple<T1, T2, T3, T4, T5, T6, T7> LoadTuple7<T1, T2, T3, T4, T5, T6, T7>(BinaryReader reader)
          => new Tuple<T1, T2, T3, T4, T5, T6, T7>(
              reader.Read<T1>(), reader.Read<T2>(), reader.Read<T3>(), reader.Read<T4>(),
              reader.Read<T5>(), reader.Read<T6>(), reader.Read<T7>()
              );

        static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> LoadTuple8<T1, T2, T3, T4, T5, T6, T7, TRest>(BinaryReader reader)
          => new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
              reader.Read<T1>(), reader.Read<T2>(), reader.Read<T3>(), reader.Read<T4>(),
              reader.Read<T5>(), reader.Read<T6>(), reader.Read<T7>(), reader.Read<TRest>()
              );

        static void WriteTuple1<T1>(BinaryWriter writer, Tuple<T1> value)
          => writer.WriteAs(value.Item1);

        static void WriteTuple2<T1, T2>(BinaryWriter writer, Tuple<T1, T2> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
        }

        static void WriteTuple3<T1, T2, T3>(BinaryWriter writer, Tuple<T1, T2, T3> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
            writer.WriteAs(value.Item3);
        }

        static void WriteTuple4<T1, T2, T3, T4>(BinaryWriter writer, Tuple<T1, T2, T3, T4> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
            writer.WriteAs(value.Item3);
            writer.WriteAs(value.Item4);
        }

        static void WriteTuple5<T1, T2, T3, T4, T5>(BinaryWriter writer, Tuple<T1, T2, T3, T4, T5> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
            writer.WriteAs(value.Item3);
            writer.WriteAs(value.Item4);
            writer.WriteAs(value.Item5);
        }

        static void WriteTuple6<T1, T2, T3, T4, T5, T6>(BinaryWriter writer, Tuple<T1, T2, T3, T4, T5, T6> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
            writer.WriteAs(value.Item3);
            writer.WriteAs(value.Item4);
            writer.WriteAs(value.Item5);
            writer.WriteAs(value.Item6);
        }

        static void WriteTuple7<T1, T2, T3, T4, T5, T6, T7>(BinaryWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
            writer.WriteAs(value.Item3);
            writer.WriteAs(value.Item4);
            writer.WriteAs(value.Item5);
            writer.WriteAs(value.Item6);
            writer.WriteAs(value.Item7);
        }

        static void WriteTuple8<T1, T2, T3, T4, T5, T6, T7, TRest>(BinaryWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> value)
        {
            writer.WriteAs(value.Item1);
            writer.WriteAs(value.Item2);
            writer.WriteAs(value.Item3);
            writer.WriteAs(value.Item4);
            writer.WriteAs(value.Item5);
            writer.WriteAs(value.Item6);
            writer.WriteAs(value.Item7);
            writer.WriteAs(value.Rest);
        }
    }
}
