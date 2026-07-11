using System;
using System.Text.RegularExpressions;
using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.IO.Streams;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class AssetContainer<AssetType>
        : Disposable, SysCol.IEnumerable<AssetContainer<AssetType>.Entry>
        where AssetType : class
    {
        public class CancellationToken
        {
            volatile bool _canceled = false;
            public bool canceled => _canceled;
            public void Cancel() => _canceled = true;
        }

        /// <summary>
        ///     A delegate representing a factory for <typeparamref name="AssetType"/>.
        ///     Can be canceled with the provided <see cref="CancellationToken"/>.
        ///     If canceled, this function must return either a <typeparamref name="AssetType"/>
        ///     that can safely be disposed with <see cref="AssetContainer{AssetType}.DisposeItem(AssetType)"/>
        ///     or <see langword="default"/>(<typeparamref name="AssetType"/>) in which case
        ///     <see cref="AssetContainer{AssetType}.DisposeItem(AssetType)"/> is not called.
        /// </summary>
        /// <param name="cancel">
        ///     The <see cref="CancellationToken"/> to be used for cancellation.
        ///     If <see langword="null"/> the <see cref="Factory"/> cannot be canceled.
        /// </param>
        /// <returns>
        ///     A valid <typeparamref name="AssetType"/> if the factory was not canceled.
        ///     <see langword="null"/> or a safely disposable <typeparamref name="AssetType"/> otherwise.
        /// </returns>
        public delegate AssetType Factory(CancellationToken cancel);

        public readonly StreamSource streamSource;
        public readonly bool backgroundLoading;

        readonly Entry.LoadKillPair loadKillForFactory;
        readonly Entry.LoadKillPair loadKillForStream;

        readonly MonitoringWorker monitoringWorker;

        readonly SysCol.Dictionary<Key, Entry> entries = new SysCol.Dictionary<Key, Entry>();
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

        public SysCol.IEnumerable<Entry> content => entries.Values;
        public SysCol.IEnumerable<Key> keys => entries.Keys;

        public AssetContainer(
            StreamSource streamSource,
            bool monitoring,
            bool backgroundLoading = false,
            Factory defaultGenerator = null
            )
        {
            loadKillForFactory = new Entry.LoadKillPair(LoadFromFactory, DisposeItem);
            loadKillForStream = new Entry.LoadKillPair(LoadFromStreamInternal, DisposeItem);

            _defaultGenerator = defaultGenerator;
            this.streamSource = streamSource;
            this.backgroundLoading = backgroundLoading;
            defaultValue = Entry.Mock(GenerateDefault, DisposeDefault);
            if (monitoring)
                monitoringWorker = new MonitoringWorker(this);
        }

        public bool ContainsKey(Key key) => entries.ContainsKey(key);
        public bool ContainsKey(string key) => ContainsKey(GenerateKey(key));

        AssetType GenerateDefault(Key key, CancellationToken cancel)
            => _defaultGenerator == null ? null : _defaultGenerator(null);

        protected virtual Key GenerateKey(string path) => new Key(path);

        public void LoadAll(Disposable monitor1, params Disposable[] monitors)
            => LoadAll(GenerateKey, Linq.PredicateTrue, monitor1, monitors);

        public void LoadAll(string regex, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(GenerateKey, new Regex(regex, RegexOptions.Compiled), monitor1, monitors);

        public void LoadAll(Regex regex, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(GenerateKey, k => regex.IsMatch(k.key), monitor1, monitors);

        public void LoadAll(Func<string, Key> generateKey, Regex regex, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(generateKey, k => regex.IsMatch(k.key), monitor1, monitors);

        public void LoadAll(Predicate<Key> load, Disposable monitor1, params Disposable[] monitors)
            => LoadAll(GenerateKey, load, monitor1, monitors);

        public void LoadAll(Func<string, Key> generateKey, Predicate<Key> load, Disposable monitor1, params Disposable[] monitors)
        {
            foreach (string streamSourceKey in streamSource.EnumerateKeys())
            {
                Key assetKey = generateKey(streamSourceKey);
                if (load(assetKey))
                    Load(assetKey, monitor1, monitors);
            }
        }

        public virtual bool TryLoad(string key, out Entry loaded, Disposable monitor1, params Disposable[] monitors)
            => TryLoad(GenerateKey(key), out loaded, monitor1, monitors);

        protected bool TryLoad(Key key, out Entry returnVal, Disposable monitor1, params Disposable[] monitors)
        {
            lock (entries)
            {
                if (entries.TryGetValue(key, out returnVal))
                {
                    returnVal.AddMonitors(monitor1, monitors);
                    return true;
                }

                try
                {
                    if (factories.ContainsKey(key))
                        returnVal = new Entry(this, key, loadKillForFactory, monitor1, monitors);
                    else if (streamSource.ContainsKey(key.key))
                        returnVal = new Entry(this, key, loadKillForStream, monitor1, monitors);
                    else
                        return false;

                    entries.Add(key, returnVal);
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
            lock (entries)
            {
                defaultValue.RefreshContent();

                foreach (SysCol.KeyValuePair<Key, Entry> pair in entries)
                    pair.Value.RefreshContent();
            }
        }

        void RemoveEntry(Key key)
        {
            lock (entries)
            {
                Entry entry = entries[key];
                entries.Remove(key);
                entry.DisposeContent();
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

        /// <summary>
        ///     Loads a <typeparamref name="AssetType"/> from the provided <paramref name="resource"/> stream.
        ///     Can be canceled with the provided <see cref="CancellationToken"/>.
        ///     If canceled, this function must return either a <typeparamref name="AssetType"/>
        ///     that can safely be disposed with <see cref="AssetContainer{AssetType}.DisposeItem(AssetType)"/>
        ///     or <see langword="default"/>(<typeparamref name="AssetType"/>) in which case
        ///     <see cref="AssetContainer{AssetType}.DisposeItem(AssetType)"/> is not called.
        /// </summary>
        /// <param name="key"> The key to retrieve the asset for. </param>
        /// <param name="resource"> The stream to load the asset from. </param>
        /// <param name="cancel">
        ///     The <see cref="CancellationToken"/> to be used for cancellation.
        ///     If <see langword="null"/> the load procedure cannot be canceled.
        /// </param>
        /// <returns>
        ///     A valid <typeparamref name="AssetType"/> if the load procedure was not canceled.
        ///     <see langword="null"/> or a safely disposable <typeparamref name="AssetType"/> otherwise.
        /// </returns>
        protected abstract AssetType LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel);

        public virtual void Dispose(string key)
            => Dispose(GenerateKey(key));

        public void Dispose(Key key)
        {
            if (entries.ContainsKey(key))
            {
                Entry entry;
                if (entries.TryGetValue(key, out entry))
                {
                    entries.Remove(key);
                    entry.DisposeContent();
                }
            }
        }

        void DisposeDefault(AssetType defaultValue)
        {
            if (defaultValue != null)
                DisposeItem(defaultValue);
        }

        protected abstract void DisposeItem(AssetType obj);

        protected override void DoDispose()
        {
            monitoringWorker?.Dispose();
            base.DoDispose();

            lock (entries)
            {
                defaultValue.DisposeContent();

                foreach (SysCol.KeyValuePair<Key, Entry> pair in entries)
                    pair.Value.DisposeContent();

                entries.Clear();
                factories.Clear();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (SysCol.KeyValuePair<Key, Entry> kvp in entries)
                yield return kvp.Value;
        }

        SysCol.IEnumerator<Entry> SysCol.IEnumerable<Entry>.GetEnumerator()
        {
            foreach (SysCol.KeyValuePair<Key, Entry> kvp in entries)
                yield return kvp.Value;
        }
    }
}
