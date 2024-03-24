namespace ChaosFramework.IO.Containers
{
    public abstract partial class DataContainer<DataType>
    {
        [System.Diagnostics.DebuggerDisplay(nameof(key) + "={" + nameof(key) + "}")]
        public class Key
        {
            public readonly string key;
            internal readonly string comparisonKey;
            internal readonly string shortComparisonKey;

            protected internal Key(string key)
            {
                this.key = key;
                comparisonKey = ChaosUtil.Platform.Paths.Normalization.NormalizeFullPath(key);
                shortComparisonKey = key.ToLower();
            }

            public static bool operator ==(Key a, Key b)
                => Collections.Util.CheckEquality(a, b);

            public static bool operator !=(Key a, Key b) => !(a == b);

            public override bool Equals(object obj)
                => Equals(obj as Key);

            public bool Equals(Key other)
                => (object)other != null
                && (shortComparisonKey.Equals(other.shortComparisonKey) || comparisonKey.Equals(other.comparisonKey));

            public override int GetHashCode() => comparisonKey.GetHashCode();
        }
    }
}
