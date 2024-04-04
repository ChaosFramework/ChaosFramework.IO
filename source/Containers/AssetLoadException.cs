using System;

namespace ChaosFramework.IO.Containers
{
    public class AssetLoadException<T> : Exception
    {
        public readonly AssetContainer<T>.Key key;

        public AssetLoadException(AssetContainer<T>.Key key, Exception innerException = null)
            : base($"Could not load asset for key \"{key.key}\".", innerException)
        {
            this.key = key;
        }
    }
}
