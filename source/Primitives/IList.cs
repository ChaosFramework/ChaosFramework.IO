using System;
using System.IO;
using System.Reflection;
using static ChaosFramework.IO.ChaosIO;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Primitives
{
    static class IList
    {
        const BindingFlags PRIVATE_STATIC = BindingFlags.NonPublic | BindingFlags.Static;

        static readonly MethodInfo readMethod = typeof(IList).GetMethod(nameof(LoadIList), PRIVATE_STATIC);
        static readonly MethodInfo writeMethod = typeof(IList).GetMethod(nameof(WriteIList), PRIVATE_STATIC);

        public static void RegisterIO()
        {
            genericReaders.Add(MakeGenericReader);
            genericWriters.Add(MakeGenericWriter);
        }

        static Reader MakeGenericReader(System.Type type)
        {
            // TODO: make this work if concrete type's generic parameters differ from the IList it implements
            // TODO: throw exception, if type implements multiple ILists

            if (!type.IsGenericType)
                return null;

            System.Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 1)
                return null;

            if (genericArguments[0].IsGenericParameter)
                return null;

            MethodInfo genericReader = readMethod.MakeGenericMethod(new System.Type[] { type, genericArguments[0] });
            return reader => genericReader.Invoke(null, new object[] { reader });
        }

        static Writer MakeGenericWriter(System.Type type)
            => ChaosUtil.Reflection.Types.ImplementsOrInheritsGenericTypeDefinition(type, typeof(SysCol.IList<>))
                ? ChaosIO.MakeGenericWriter(type, writeMethod)
                : null;

        static ListType LoadIList<ListType, ContentType>(BinaryReader reader)
            where ListType : SysCol.IList<ContentType>
        {
            ListType lst = Activator.CreateInstance<ListType>();
            int count = reader.Read<int>();
            for (int i = 0; i < count; i++)
                lst.Add(reader.Read<ContentType>());

            return lst;
        }

        static void WriteIList<T>(BinaryWriter writer, SysCol.IList<T> value)
        {
            writer.WriteAs(value.Count);
            foreach (T obj in value)
                writer.WriteAs<T>(obj);
        }
    }
}
