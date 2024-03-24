using System;
using System.IO;
using System.Reflection;

namespace ChaosFramework.IO
{
    public partial class TypedBinaryReader : BinaryReader
    {
        public readonly TypeMappingMethod mappingMethod;

        internal readonly TypeMap typeMap;
        long typeMapLength = 0;

        public long effectiveLength => BaseStream.Length - typeMapLength;

        public TypedBinaryReader(Stream baseStream, TypeMappingMethod mappingMethod = TypeMappingMethod.SingleStore)
            : this(baseStream, System.Text.Encoding.UTF8, mappingMethod)
        { }

        public TypedBinaryReader(
            Stream baseStream,
            System.Text.Encoding encoding,
            TypeMappingMethod mappingMethod = TypeMappingMethod.SingleStore
            ) : base(baseStream, encoding)
        {
            this.mappingMethod = mappingMethod;
            typeMap = new TypeMap(mappingMethod == TypeMappingMethod.Sequential);
            if (mappingMethod == TypeMappingMethod.SingleStore)
                InitializeTypeTable();
        }

        public TypedBinaryReader(string fileName, FileMode access = FileMode.Open)
            : this(new FileStream(fileName, access))
        { }

        void InitializeTypeTable()
        {
            try
            {
                long previousPosition = BaseStream.Position + 8;
                BaseStream.Position = ReadInt64();
                typeMapLength = BaseStream.Length - BaseStream.Position;
                typeMap.Read(this);
                BaseStream.Position = previousPosition;
            }
            catch
            {
                Close();
                throw;
            }
        }

        public Type ReadType()
        {
            ushort index = ReadUInt16();
            if (index == ushort.MaxValue) return null;
            if (index >= typeMap.readTypes.Length)
                if (mappingMethod == TypeMappingMethod.Sequential)
                    typeMap.ReadDynamicType(this);
                else
                    throw new InvalidDataException($"Could not identify type 0x{index.ToString("X4")}.");

            return typeMap.readTypes[index];
        }

        public MethodInfo ReadMethod()
        {
            ushort index = ReadUInt16();
            if (index == ushort.MaxValue) return null;
            if (index >= typeMap.readMethods.Length)
                if (mappingMethod == TypeMappingMethod.Sequential)
                    typeMap.ReadDynamicMethod(this);
                else
                    throw new InvalidDataException($"Could not identify method 0x{index.ToString("X4")}.");

            return typeMap.readMethods[index];
        }
    }
}
