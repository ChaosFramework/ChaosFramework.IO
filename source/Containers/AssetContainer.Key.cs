namespace ChaosFramework.IO.Containers
{
    public abstract partial class AssetContainer<AssetType>
    {
        [System.Diagnostics.DebuggerDisplay(nameof(key) + "={" + nameof(key) + "}")]
        public class Key
        {
            public readonly string key;

            protected internal Key(string key)
            {
                this.key = ChaosUtil.Platform.Paths.Normalization.NormalizeRelative(key);
            }

            public static bool operator ==(Key a, Key b)
                => Collections.Util.CheckEquality(a, b);

            public static bool operator !=(Key a, Key b) => !(a == b);

            public override bool Equals(object obj)
                => Equals(obj as Key);

            public bool Equals(Key other)
                => (object)other != null
                && key.Equals(other.key);

            public override int GetHashCode() => key.GetHashCode();
        }
    }
}
