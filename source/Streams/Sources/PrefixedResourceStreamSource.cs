using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public override IEnumerable<string> EnumerateKeys()
            => from System.Collections.DictionaryEntry resource in resourceSet
               let resourceName = (string)resource.Key
               where resourceName.StartsWith(resourceNamePrefix)
               select ResourceNameToKey(resourceName);

        protected override string KeyToResourceName(string key)
        {
            if (key.StartsWith(keyPrefix))
                return $"{resourceNamePrefix}{key.Substring(keyPrefix.Length)}";
            else
                throw new KeyFormatException(
                    key,
                    new System.ArgumentException(
                        $"Key must start with key prefix \"{keyPrefix}\".",
                        nameof(key)
                        )
                    );
        }

        protected override string ResourceNameToKey(string resourceName)
        {
            if (resourceName.StartsWith(resourceNamePrefix))
                return $"{keyPrefix}{resourceName.Substring(resourceNamePrefix.Length)}";
            else
                throw new KeyFormatException(
                    resourceName,
                    new System.ArgumentException(
                        $"Resource name must start with resource name prefix \"{resourceNamePrefix}\".",
                        nameof(resourceName)
                        )
                    );
        }
    }
}
