using ChaosFramework.Core;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class ParameterizedAssetContainer<DataType, ParameterType> : AssetContainer<DataType>
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

        public void LoadDirectory(
            string directory,
            ParameterType param,
            string[] fileExtensions,
            bool recursive,
            Disposable monitor1,
            params Disposable[] monitors
            )
            => LoadDirectory(name => new ParameterizedKey(name, param), directory, fileExtensions, recursive, monitor1, monitors);
    }
}
