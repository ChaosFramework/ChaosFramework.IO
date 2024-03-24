using System.IO;
using System.Reflection;
using static ChaosFramework.IO.ChaosIO;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Primitives
{
    static class Dictionary
    {
        const BindingFlags PRIVATE_STATIC = BindingFlags.NonPublic | BindingFlags.Static;

        static readonly MethodInfo writeMethod = typeof(Dictionary).GetMethod(nameof(Write), PRIVATE_STATIC);
        static readonly MethodInfo readMethod = typeof(Dictionary).GetMethod(nameof(Read), PRIVATE_STATIC);

        public static void RegisterIO()
        {
            genericWriters.Add(MakeGenericWriter);
            genericReaders.Add(MakeGenericReader);
        }

        static Writer MakeGenericWriter(System.Type type)
            => ChaosUtil.Reflection.Types.ImplementsOrInheritsGenericTypeDefinition(type, typeof(SysCol.Dictionary<,>))
                ? ChaosIO.MakeGenericWriter(type, writeMethod)
                : null;

        static Reader MakeGenericReader(System.Type type)
            => ChaosUtil.Reflection.Types.ImplementsOrInheritsGenericTypeDefinition(type, typeof(SysCol.Dictionary<,>))
                ? ChaosIO.MakeGenericReader(type, readMethod)
                : null;

        static SysCol.Dictionary<K, V> Read<K, V>(BinaryReader reader)
        {
            SysCol.Dictionary<K, V> output = new SysCol.Dictionary<K, V>();
            int count = reader.Read<int>();
            for (int i = 0; i < count; i++)
                output[reader.Read<K>()] = reader.Read<V>();
            return output;
        }

        static void Write<K, V>(BinaryWriter writer, SysCol.Dictionary<K, V> value)
        {
            writer.WriteAs(value.Count);
            foreach (SysCol.KeyValuePair<K, V> pair in value)
            {
                writer.WriteAs(pair.Key);
                writer.WriteAs(pair.Value);
            }
        }
    }
}
