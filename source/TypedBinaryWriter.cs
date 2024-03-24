using System;
using System.IO;
using System.Reflection;

namespace ChaosFramework.IO
{
    public partial class TypedBinaryWriter : BinaryWriter
    {
        public readonly TypeMappingMethod mappingMethod;

        internal readonly TypeMap typeMap;

        bool closed = false;
        bool hasParent;
        long typeTablePointerPosition = 0;

        public TypedBinaryWriter(Stream baseStream, TypeMappingMethod mappingMethod = TypeMappingMethod.SingleStore)
            : base(baseStream)
        {
            this.mappingMethod = mappingMethod;
            hasParent = false;
            typeMap = new TypeMap(mappingMethod == TypeMappingMethod.Sequential);
            typeTablePointerPosition = baseStream.Position;
            if (mappingMethod == TypeMappingMethod.SingleStore)
                base.Write(0L);
        }

        public TypedBinaryWriter(Stream baseStream, System.Text.Encoding encoding, bool dynamic)
            : base(baseStream, encoding)
        {
            hasParent = false;
            typeMap = new TypeMap(dynamic);
            typeTablePointerPosition = baseStream.Position;
            if (!dynamic)
                base.Write(0L);
        }

        public TypedBinaryWriter(string fileName, FileMode access = FileMode.Create)
            : this(new FileStream(fileName, access))
        { }

        public void Write(Type t)
        {
            if (t == null)
                Write(ushort.MaxValue);
            else if (typeMap.sequential)
                typeMap.WriteDynamicHash(t, this, hash => Write(hash));
            else
                Write(typeMap.GetHash(t));
        }

        public void Write(MethodInfo m)
        {
            if (m == null)
                Write(ushort.MaxValue);
            else if (typeMap.sequential)
                typeMap.WriteDynamicHash(m, this, hash => Write(hash));
            else
                Write(typeMap.GetHash(m));
        }

        public override void Close()
        {
            if (closed)
                throw new InvalidOperationException("Writer was already closed.");

            closed = true;
            if (!(hasParent || typeMap.sequential) && mappingMethod == TypeMappingMethod.SingleStore)
            {
                long startOfTypeTable = BaseStream.Position = BaseStream.Length;
                typeMap.SaveDelta(this);
                BaseStream.Position = typeTablePointerPosition;
                Write(startOfTypeTable);
            }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (!closed)
                Close();

            base.Dispose(disposing);
        }
    }
}
