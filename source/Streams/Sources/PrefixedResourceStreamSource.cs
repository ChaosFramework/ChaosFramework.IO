using System.Globalization;
using System.Resources;

namespace ChaosFramework.IO.Streams.Sources
{
    public class PrefixedResourceStreamSource : ResourceStreamSource
    {
        public readonly string keyPrefix;
        public readonly string resourceNamePrefix;

        public PrefixedResourceStreamSource(
            string resourceNamePrefix,
            string keyPrefix,
            ResourceManager resources,
            CultureInfo culture = null
            )
            : base(resources, culture)
        {
            this.resourceNamePrefix = resourceNamePrefix ?? string.Empty;
            this.keyPrefix = keyPrefix ?? string.Empty;
        }

        public override string KeyToResourceName(string key)
            => $"{resourceNamePrefix}{key.Substring(keyPrefix.Length)}";

        public override string ResourceNameToKey(string resourceName)
            => $"{keyPrefix}{resourceName.Substring(resourceNamePrefix.Length)}";
    }
}
