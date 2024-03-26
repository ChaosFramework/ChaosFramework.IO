using ChaosFramework.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;

namespace ChaosFramework.IO.Streams.Sources
{
    public class ResourceStreamSource : StreamSource
    {
        public readonly ResourceManager resources;
        public readonly ResourceSet resourceSet;
        public readonly CultureInfo culture;

        public ResourceStreamSource(ResourceManager resources, CultureInfo culture = null)
        {
            this.resources = resources;
            this.culture = culture ?? CultureInfo.CurrentCulture;
            resourceSet = resources.GetResourceSet(this.culture, true, false);
        }

        IEnumerable<string> StreamSource.EnumerateKeys()
            => from System.Collections.DictionaryEntry resource in resourceSet
               select ResourceNameToKey((string)resource.Key);

        bool StreamSource.ContainsKey(string key)
            => ((StreamSource)this).EnumerateKeys(key).NotEmpty();

        Stream StreamSource.OpenRead(string key)
        {
            IEnumerable<string> keys = ((StreamSource)this).EnumerateKeys(key);
            string keyWithCorrectCapitalization = keys.First();
            string resourceName = KeyToResourceName(keyWithCorrectCapitalization);
            object resource = resourceSet.GetObject(resourceName);
            return new MemoryStream((byte[])resource);
        }

        public virtual string ResourceNameToKey(string resourceName) => resourceName;

        public virtual string KeyToResourceName(string key) => key;
    }
}
