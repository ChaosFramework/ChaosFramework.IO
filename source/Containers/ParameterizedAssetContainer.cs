using System;
using System.Text.RegularExpressions;
using ChaosFramework.Collections;
using ChaosFramework.Core;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class ParameterizedAssetContainer<AssetType, ParameterType> : AssetContainer<AssetType>
        where AssetType : class
    {
        public virtual ParameterType defaultParameter { get; protected set; } = default(ParameterType);

        protected override Key GenerateKey(string path) => new ParameterizedKey(path, defaultParameter);

        public ParameterizedAssetContainer(Streams.StreamSource streamSource, bool monitoring, bool backgroundLoading = false)
            : base(streamSource, monitoring, backgroundLoading)
        { }

        public override sealed Entry Load(string key, Disposable monitor1, Disposable[] monitors)
            => Load(new ParameterizedKey(key, defaultParameter), monitor1, monitors);

        public override sealed bool TryLoad(string key, out Entry result, Disposable monitor1, Disposable[] monitors)
            => TryLoad(new ParameterizedKey(key, defaultParameter), out result, monitor1, monitors);

        public Entry Load(string key, ParameterType param, Disposable monitor1, params Disposable[] monitors)
            => Load(new ParameterizedKey(key, param), monitor1, monitors);

        public bool TryLoad(string key, ParameterType param, out Entry result, Disposable monitor1, Disposable[] monitors)
            => TryLoad(new ParameterizedKey(key, param), out result, monitor1, monitors);

        public void Dispose(string key, ParameterType param)
            => Dispose(new ParameterizedKey(key, param));

        public bool ContainsKey(string key, ParameterType param)
            => ContainsKey(new ParameterizedKey(key, param));

        public void LoadAll(ParameterType param, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(param, Linq.PredicateTrue, monitor1, monitors);

        public void LoadAll(ParameterType param, string regex, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(param, new Regex(regex, RegexOptions.Compiled), monitor1, monitors);

        public void LoadAll(ParameterType param, Regex regex, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(param, k => regex.IsMatch(k.key), monitor1, monitors);

        public void LoadAll(ParameterType param, Func<string, Key> generateKey, Regex regex, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(param, k => regex.IsMatch(k.key), monitor1, monitors);

        public void LoadAll(ParameterType param, Predicate<Key> load, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(name => new ParameterizedKey(name, param), load, monitor1, monitors);
    }
}
