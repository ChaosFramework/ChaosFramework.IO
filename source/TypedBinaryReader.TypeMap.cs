using ChaosUtil.Primitives;
using ChaosUtil.Reflection;
using System;
using System.Reflection;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO
{
    public partial class TypedBinaryReader
    {
        public class TypeMap
        {
            class MissingType { }

            public static SysCol.Dictionary<string, string> debugMappings = new SysCol.Dictionary<string, string>();

            internal readonly bool dynamic;

            // TODO: don't use arrays for this?!
            internal Type[] readTypes = Array<Type>.empty;
            internal MethodInfo[] readMethods = Array<MethodInfo>.empty;

            internal TypeMap(bool dynamic) { this.dynamic = dynamic; }

            public void Read(TypedBinaryReader rd)
            {
                ushort typeCount = rd.ReadUInt16();
                int offset = readTypes.Length;
                Array.Resize(ref readTypes, readTypes.Length + typeCount);
                for (int i = 0; i < typeCount; i++)
                {
                    string typeName = rd.ReadString();
                    string originalTypeName = typeName;
                    Type t;
                    if (Types.TryParseType(typeName, out t)
                        || (debugMappings.TryGetValue(typeName, out typeName) && Types.TryParseType(typeName, out t))
                        )
                        readTypes[i + offset] = t;
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"{nameof(TypedBinaryReader)} could not find type \"{originalTypeName}\".");
                        readTypes[i + offset] = typeof(MissingType);
                    }
                }

                ushort methodCount = rd.ReadUInt16();
                offset = readMethods.Length;
                Array.Resize(ref readMethods, readMethods.Length + methodCount);
                for (int i = 0; i < methodCount; i++)
                {
                    Type declaringType = rd.ReadType();
                    string name = rd.ReadString();
                    Type[] parameters = new Type[rd.ReadByte()];
                    for (int k = 0; k < parameters.Length; k++)
                        parameters[k] = rd.ReadType();

                    readMethods[i + offset] = declaringType.GetMethod(
                        name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        parameters,
                        null
                        );
                }
            }

            internal void ReadDynamicType(TypedBinaryReader rd)
            {
                Array.Resize(ref readTypes, readTypes.Length + 1);
                string typeName = rd.ReadString();
                Type t;
                if (!AssemblyManager.TryGetTypeByFullName(typeName, out t))
                    throw new InvalidOperationException($"File contains type \"{typeName}\" that is not found in running assemblies.");

                readTypes[readTypes.Length - 1] = t;
            }

            internal void ReadDynamicMethod(TypedBinaryReader rd)
            {
                Array.Resize(ref readMethods, readMethods.Length + 1);
                Type declaringType = rd.ReadType();
                string name = rd.ReadString();
                Type[] parameters = new Type[rd.ReadByte()];
                for (int i = 0; i < parameters.Length; i++)
                    parameters[i] = rd.ReadType();

                readMethods[readMethods.Length - 1] = declaringType.GetMethod(name, parameters);
            }
        }
    }
}
