namespace ChaosFramework.IO.Containers
{
    public abstract partial class ParameterizedDataContainer<DataType, ParameterType>
    {
        [System.Diagnostics.DebuggerDisplay(nameof(key) + "={" + nameof(key) + "}, " + nameof(param) + "={" + nameof(param) + "}")]
        public class ParameterizedKey : Key
        {
            public readonly ParameterType param;

            public ParameterizedKey(string key, ParameterType param)
                : base(key)
            { this.param = param; }

            public static bool operator ==(ParameterizedKey a, ParameterizedKey b)
                => Collections.Util.CheckEquality(a, b);

            public static bool operator !=(ParameterizedKey a, ParameterizedKey b) => !(a == b);

            public override bool Equals(object obj)
                => Equals(obj as ParameterizedKey);

            public bool Equals(ParameterizedKey other)
                => base.Equals(other)
                && Collections.Util.CheckEquality(param, other.param);

            public override int GetHashCode()
                => base.GetHashCode() ^ param.GetHashCode();
        }
    }
}
