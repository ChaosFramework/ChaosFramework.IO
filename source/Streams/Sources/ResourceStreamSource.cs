using ChaosFramework.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;

namespace ChaosFramework.IO.Streams.Sources
{
    public class ResourceStreamSource : StreamSource
    {
        public readonly ResourceSet resourceSet;
        public readonly CultureInfo culture;

        bool alive = true;

        public ResourceStreamSource(ResourceManager resources, CultureInfo culture = null)
        {
            if (resources == null)
                throw new System.ArgumentNullException(nameof(resources));

            this.culture = culture ?? CultureInfo.CurrentCulture;
            resourceSet = resources.GetResourceSet(this.culture, true, false);

            if (resourceSet == null)
                throw new StreamSourceException("Resource set not found!");
        }

        public virtual IEnumerable<string> EnumerateKeys()
            => from System.Collections.DictionaryEntry resource in resourceSet
               select ResourceNameToKey((string)resource.Key);

        bool StreamSource.ContainsKey(string key)
            => ((StreamSource)this).EnumerateKeys(key).NotEmpty();

        Stream StreamSource.OpenRead(string key)
        {
            IEnumerable<string> keys = ((StreamSource)this).EnumerateKeys(key);
            string keyWithCorrectCapitalization = keys.FirstOrDefault();
            if (keyWithCorrectCapitalization == null)
                throw new KeyNotFoundException(key);

            string resourceName = KeyToResourceName(keyWithCorrectCapitalization);
            try
            {
                object resource = resourceSet.GetObject(resourceName);
                return new MemoryStream((byte[])resource);
            }
            catch (System.Exception ex)
            {
                throw new StreamAccessException(key, ex);
            }
        }

        protected virtual string ResourceNameToKey(string resourceName) => resourceName;

        protected virtual string KeyToResourceName(string key) => key;

        bool StreamSource.alive
        {
            get
            {
                if (alive)
                    try
                    {
                        // throws an ObjectDisposedException if resourceSet is disposed
                        resourceSet.GetEnumerator().MoveNext();

                        return true;
                    }
                    catch (System.ObjectDisposedException)
                    {
                        return alive = false;
                    }
                else
                    return false;
            }
        }
    }
}
