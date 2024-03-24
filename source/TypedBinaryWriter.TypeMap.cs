using ChaosFramework.Collections;
using System;
using System.Reflection;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO
{
    public partial class TypedBinaryWriter
    {
        public class TypeMap
        {
            internal delegate void DynamicHashWriter(ushort hash);

            static ushort GetHash<T>(T entry, LinkedList<T> delta, SysCol.Dictionary<T, ushort> recorded)
            {
                ushort hash;
                if (!recorded.TryGetValue(entry, out hash))
                {
                    delta.Add(entry);
                    hash = (ushort)recorded.Count;
                    recorded[entry] = hash;
                }
                return hash;
            }

            static void WriteDynamicHash<T>(
                TypedBinaryWriter wr,
                T entry,
                LinkedList<T> delta,
                SysCol.Dictionary<T, ushort> recorded,
                DynamicHashWriter writeHash
                )
            {
                ushort hash;
                if (!recorded.TryGetValue(entry, out hash))
                {
                    hash = (ushort)recorded.Count;
                    delta.Add(entry);
                    recorded[entry] = hash;
                    writeHash(hash);
                    wr.Write(entry.ToString());
                }
                else
                    writeHash(hash);
            }

            internal readonly bool sequential;

            readonly LinkedList<Type> deltaTypes = new LinkedList<Type>();
            readonly LinkedList<MethodInfo> deltaMethods = new LinkedList<MethodInfo>();

            readonly SysCol.Dictionary<Type, ushort> recordedTypes = new SysCol.Dictionary<Type, ushort>();
            readonly SysCol.Dictionary<MethodInfo, ushort> recordedMethods = new SysCol.Dictionary<MethodInfo, ushort>();

            internal TypeMap(bool sequential) { this.sequential = sequential; }

            public ushort GetHash(Type t)
                => GetHash(t, deltaTypes, recordedTypes);

            public ushort GetHash(MethodInfo m)
                => GetHash(m, deltaMethods, recordedMethods);

            internal void WriteDynamicHash(Type t, TypedBinaryWriter wr, DynamicHashWriter writeHash)
                => WriteDynamicHash(wr, t, deltaTypes, recordedTypes, writeHash);

            internal void WriteDynamicHash(MethodInfo m, TypedBinaryWriter wr, DynamicHashWriter writeHash)
                => WriteDynamicHash(wr, m, deltaMethods, recordedMethods, writeHash);

            void WriteMethod(TypedBinaryWriter wr, MethodInfo m)
            {
                wr.Write(m.DeclaringType);
                wr.Write(m.Name);
                ParameterInfo[] parameters = m.GetParameters();
                wr.Write((byte)parameters.Length);
                foreach (ParameterInfo param in parameters)
                    wr.Write(param.ParameterType);
            }

            /// <summary>
            ///     Determines if the typemap has changed since the last call to <see cref="SaveDelta(TypedBinaryWriter)"/>.
            /// </summary>
            /// <returns>
            ///     <see langword="true"/> if the <see cref="TypeMap"/> has changed
            ///     since the last call to <see cref="SaveDelta(TypedBinaryWriter)"/>;
            ///     <see langword="false"/> otherwise.
            /// </returns>
            public bool HasDelta()
                => deltaMethods.length > 0 || deltaTypes.length > 0;

            /// <summary>
            ///     Writes all types and methods that have been collected
            ///     since the last call of <see cref="SaveDelta(TypedBinaryWriter)"/>
            ///     into the base stream of <paramref name="wr"/>.
            /// </summary>
            /// <param name="wr"> The <see cref="TypedBinaryWriter"/> to write the delta to. </param>
            public void SaveDelta(TypedBinaryWriter wr)
            {
                foreach (MethodInfo method in deltaMethods)
                {
                    if (!recordedTypes.ContainsKey(method.DeclaringType))
                    {
                        recordedTypes[method.DeclaringType] = (ushort)recordedTypes.Count;
                        deltaTypes.Add(method.DeclaringType);
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    foreach (ParameterInfo param in parameters)
                        if (!recordedTypes.ContainsKey(param.ParameterType))
                        {
                            recordedTypes[param.ParameterType] = (ushort)recordedTypes.Count;
                            deltaTypes.Add(param.ParameterType);
                        }
                }

                // Write types
                wr.Write((ushort)deltaTypes.length);
                foreach (Type toBeHashed in deltaTypes)
                    wr.Write(toBeHashed.ToString());

                // Write method infos
                wr.Write((ushort)deltaMethods.length);
                foreach (MethodInfo toBeHashed in deltaMethods)
                    WriteMethod(wr, toBeHashed);

                deltaTypes.Clear();
                deltaMethods.Clear();
            }
        }
    }
}
