using ChaosFramework.Collections;
using ChaosUtil.Primitives;
using ChaosUtil.Reflection;
using System;
using System.IO;
using BindingFlags = System.Reflection.BindingFlags;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO
{
    public static class ChaosIO
    {
        [AttributeUsage(AttributeTargets.Method)]
        public class RegisterTypeAttribute : Attribute
        {
            public const BindingFlags REGISTER_METHOD_BINDING = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        }

        public delegate object Reader(BinaryReader reader);
        public delegate T Reader<T>(BinaryReader reader);
        public delegate void Writer(BinaryWriter writer, object value);
        public delegate void Writer<T>(BinaryWriter writer, T value);
        internal delegate Writer GenericWriter(Type t);
        internal delegate Reader GenericReader(Type t);

        internal static readonly LinkedList<GenericWriter> genericWriters = new LinkedList<GenericWriter>();
        internal static readonly LinkedList<GenericReader> genericReaders = new LinkedList<GenericReader>();
        static readonly SysCol.Dictionary<Type, Writer> writers = new SysCol.Dictionary<Type, Writer>();
        static readonly SysCol.Dictionary<Type, Reader> readers = new SysCol.Dictionary<Type, Reader>();

        static bool initialized = false;
        public static void Init()
        {
            if (initialized)
                return;
            initialized = true;

            Primitives.BuiltIn.RegisterIO();
            Primitives.DotNet.RegisterIO();
            Primitives.String.RegisterIO();

            AddType(BitHash.Read, BitHash.Write);
            AddType(BitArray.Read, BitArray.Write);

            Primitives.Dictionary.RegisterIO();
            Primitives.IList.RegisterIO();
            Primitives.Tuple.RegisterIO();
            Primitives.Type.RegisterIO();

            // RegisterIO-Attributes
            foreach (System.Reflection.Assembly ass in AssemblyManager.EnumerateRelevantAssemblies())
                foreach (Type type in ass.GetTypes())
                    foreach (System.Reflection.MethodInfo method in type.GetMethods(RegisterTypeAttribute.REGISTER_METHOD_BINDING))
                        if (method.GetAttributes<RegisterTypeAttribute>(false).Length != 0)
                            method.Invoke(null, Array<object>.empty);
        }

        internal static Writer MakeGenericWriter(Type genericType, System.Reflection.MethodInfo method)
        {
            System.Reflection.MethodInfo genericMethod = method.MakeGenericMethod(genericType.GetGenericArguments());
            return (writer, value) => genericMethod.Invoke(null, new object[] { writer, value });
        }

        internal static Reader MakeGenericReader(Type genericType, System.Reflection.MethodInfo method)
        {
            System.Reflection.MethodInfo genericMethod = method.MakeGenericMethod(genericType.GetGenericArguments());
            return reader => genericMethod.Invoke(null, new object[] { reader });
        }

        public static Reader GetReader(Type t)
        {
            Reader rd;
            if (readers.TryGetValue(t, out rd))
                return rd;

            foreach (GenericReader getReader in genericReaders)
                if ((rd = getReader(t)) != null)
                    return rd;

            return null;
        }

        public static Writer GetWriter(Type t)
        {
            Writer wr;
            if (writers.TryGetValue(t, out wr))
                return wr;

            foreach (GenericWriter getWriter in genericWriters)
                if ((wr = getWriter(t)) != null)
                    return wr;

            return null;
        }

        public static void AddType<T>(Reader<T> reader, Writer<T> writer)
            => AddType(typeof(T), rd => reader(rd), (wr, obj) => writer(wr, (T)obj));

        static void AddType(Type type, Reader reader, Writer writer)
        {
            if (readers.ContainsKey(type))
                throw new Exception($"{nameof(ChaosIO)} has already registered a reader/writer for {type}.");

            readers[type] = reader;
            writers[type] = writer;
        }

        public static bool DefinesType(Type t) => writers.ContainsKey(t);
    }
}
