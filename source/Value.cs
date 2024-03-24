using ChaosUtil.Reflection;
using System;

namespace ChaosFramework.IO
{
    public sealed class Value
    {
        public readonly Type type;

        object _value;
        public object value
        {
            get { return _value; }
            set { _value = Cast.ForceCast(type, value); }
        }

        public Value(Type type, object value)
        {
            this.type = type;
            this.value = value;
        }

        public Value(TypedBinaryReader rd)
        {
            type = rd.ReadType();
            value = rd.Read(type);
        }

        public void Save(TypedBinaryWriter wr)
        {
            wr.WriteAs(type);
            wr.WriteAs(type, value);
        }

        public Value Clone() => new Value(type, value);

        public override bool Equals(object obj)
            => Equals(obj as Value);

        public bool Equals(Value compare)
            => compare != null && compare.type == type && Collections.Util.CheckEquality(compare.value, value);

        public override int GetHashCode() => _value.GetHashCode();
    }
}
