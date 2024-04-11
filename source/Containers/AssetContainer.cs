using ChaosFramework.Core;
using ChaosFramework.IO.Streams;
using System;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class AssetContainer<AssetType> : Disposable, SysCol.IEnumerable<AssetContainer<AssetType>.Entry>
    {
        public class CancellationToken
        {
            volatile bool _canceled = false;
            public bool canceled => _canceled;
            public void Cancel() => _canceled = true;
        }

        public delegate AssetType Factory(CancellationToken cancel);

        public readonly StreamSource streamSource;
        public readonly bool backgroundLoading;

        readonly MonitoringWorker monitoringWorker;

        readonly SysCol.Dictionary<Key, Entry> data = new SysCol.Dictionary<Key, Entry>();
        readonly SysCol.Dictionary<Key, Factory> factories = new SysCol.Dictionary<Key, Factory>();

        readonly Entry defaultValue;
        Factory _defaultGenerator;
        public Factory defaultGenerator
        {
            get { return _defaultGenerator; }
            set
            {
                _defaultGenerator = value;
                defaultValue.RefreshContent();
            }
        }

        public SysCol.IEnumerable<Entry> content => data.Values;
        public SysCol.IEnumerable<Key> keys => data.Keys;

        public AssetContainer(
            StreamSource streamSource,
            bool monitoring,
            bool backgroundLoading = false,
            Factory defaultGenerator = null
            )
        {
            defaultValue = Entry.Mock(GenerateDefault);
            this.streamSource = streamSource;
            this.backgroundLoading = backgroundLoading;
            this.defaultGenerator = defaultGenerator;
            if (monitoring)
                monitoringWorker = new MonitoringWorker(this);
        }

        public bool ContainsKey(Key key) => data.ContainsKey(key);
        public bool ContainsKey(string key) => ContainsKey(GenerateKey(key));

        AssetType GenerateDefault(Key key, CancellationToken cancel)
            => _defaultGenerator == null ? default(AssetType) : _defaultGenerator(null);

        protected virtual Key GenerateKey(string path) => new Key(path);

        public void LoadDirectory(
            string directory,
            string[] fileExtensions,
            bool recursive,
            Disposable monitor1,
            params Disposable[] monitors
            )
            => LoadDirectory(GenerateKey, directory, fileExtensions, recursive, monitor1, monitors);

        protected void LoadDirectory(
            Func<string, Key> generateKey,
            string directory,
            string[] fileExtensions,
            bool recursive,
            Disposable monitor1,
            params Disposable[] monitors
            )
        {
            // TODO: Get rid of fileExtensions
            foreach (string key in streamSource.EnumerateKeys($"{directory}\\{(recursive ? "**" : "*")}"))
                foreach (string ext in fileExtensions)
                    if (key.ToLower().EndsWith(ext.ToLower()))
                    {
                        Load(generateKey(key), monitor1, monitors);
                        break;
                    }
        }

        public virtual bool TryLoad(string key, out Entry loaded, Disposable monitor1, params Disposable[] monitors)
            => TryLoad(GenerateKey(key), out loaded, monitor1, monitors);

        protected bool TryLoad(Key key, out Entry returnVal, Disposable monitor1, params Disposable[] monitors)
        {
            lock (data)
            {
                if (data.TryGetValue(key, out returnVal))
                {
                    returnVal.AddMonitors(monitor1, monitors);
                    return true;
                }

                try
                {
                    if (factories.ContainsKey(key))
                        returnVal = new Entry(this, key, LoadFromFactory, monitor1, monitors);
                    else if (streamSource.ContainsKey(key.key))
                        returnVal = new Entry(this, key, LoadFromStreamInternal, monitor1, monitors);
                    else
                        return false;

                    data.Add(key, returnVal);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{GetType()} could not load the following file: \"{key}\"", ex);
                }

                return true;
            }
        }

        public virtual Entry Load(string key, Disposable monitor1, params Disposable[] monitors)
            => Load(GenerateKey(key), monitor1, monitors);

        protected Entry Load(Key key, Disposable monitor1, Disposable[] monitors)
        {
            Entry result;
            if (!TryLoad(key, out result, monitor1, monitors))
                throw new SysCol.KeyNotFoundException($"Could not find key \"{key.key}\".");

            return result;
        }

        public virtual void AddFactory(string key, Factory factory)
            => AddFactory(GenerateKey(key), factory);

        protected void AddFactory(Key key, Factory factory)
        {
            if (ContainsKey(key) || factories.ContainsKey(key))
                throw new InvalidOperationException($"{GetType().Name} already contains a factory for \"{key.key}\".");

            factories[key] = factory;
        }

        public virtual void RefreshContent()
        {
            lock (data)
            {
                DisposeItem(defaultValue);
                defaultValue?.RefreshContent();

                foreach (SysCol.KeyValuePair<Key, Entry> pair in data)
                    pair.Value.RefreshContent();
            }
        }

        void RemoveEntry(Key key)
        {
            lock (data)
            {
                DisposeItem(data[key].content);
                data.Remove(key);
            }
        }

        AssetType LoadFromFactory(Key key, CancellationToken cancel)
        {
            Factory factory;
            if (!factories.TryGetValue(key, out factory))
                throw new KeyNotFoundException(
                    key.key,
                    new Exception($"Key no longer exists!")
                    );

            return factory(cancel);
        }

        AssetType LoadFromStreamInternal(Key key, CancellationToken cancel)
        {
            if (!streamSource.ContainsKey(key.key))
                throw new KeyNotFoundException(
                    key.key,
                    new Exception($"Key no longer exists!")
                    );

            using (System.IO.Stream resource = streamSource.OpenRead(key.key))
            {
                if (!resource.CanRead)
                    throw new StreamAccessException(
                        key.key,
                        new Exception("Resource stream is unreadable. It was likely closed.")
                        );

                AssetType obj;
                try
                {
                    obj = LoadFromStream(key, resource, cancel);
                }
                catch (Exception ex)
                {
                    throw new AssetLoadException<AssetType>(key, ex);
                }

                return obj;
            }
        }

        protected abstract AssetType LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel);

        public virtual void Dispose(string key)
            => Dispose(GenerateKey(key));

        public void Dispose(Key key)
        {
            if (data.ContainsKey(key))
            {
                Entry entry;
                if (data.TryGetValue(key, out entry))
                {
                    DisposeItem(entry);
                    data.Remove(key);
                }
            }
        }

        protected abstract void DisposeItem(AssetType obj);

        protected override void DoDispose()
        {
            monitoringWorker?.Dispose();
            base.DoDispose();

            lock (data)
            {
                DisposeItem(defaultValue);

                foreach (SysCol.KeyValuePair<Key, Entry> pair in data)
                    DisposeItem(pair.Value.content);

                data.Clear();
                factories.Clear();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (SysCol.KeyValuePair<Key, Entry> kvp in data)
                yield return kvp.Value;
        }

        SysCol.IEnumerator<Entry> SysCol.IEnumerable<Entry>.GetEnumerator()
        {
            foreach (SysCol.KeyValuePair<Key, Entry> kvp in data)
                yield return kvp.Value;
        }
    }
}
