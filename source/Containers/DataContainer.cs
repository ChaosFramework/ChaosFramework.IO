using ChaosFramework.Core;
using ChaosFramework.IO.Streams;
using System;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class DataContainer<DataType> : Disposable, SysCol.IEnumerable<DataContainer<DataType>.Entry>
    {
        public delegate DataType Factory();

        public readonly StreamSource streamSource;
        public readonly bool backgroundLoading;

        readonly MonitoringWorker monitoringWorker;

        readonly SysCol.Dictionary<Key, Entry> data = new SysCol.Dictionary<Key, Entry>();
        readonly SysCol.Dictionary<Key, Factory> custom = new SysCol.Dictionary<Key, Factory>();

        DataType defaultValue;
        Factory _defaultGenerator;
        public Factory defaultGenerator
        {
            get { return _defaultGenerator; }
            set
            {
                if (defaultValue != null)
                    DisposeItem(defaultValue);
                _defaultGenerator = value;
                if (value != null)
                    defaultValue = value();
            }
        }

        public SysCol.IEnumerable<Entry> content => data.Values;
        public SysCol.IEnumerable<Key> keys => data.Keys;

        public DataContainer(
            StreamSource streamSource,
            bool monitoring,
            bool backgroundLoading = false,
            Factory defaultGenerator = null
            )
        {
            this.streamSource = streamSource;
            this.backgroundLoading = backgroundLoading;
            this.defaultGenerator = defaultGenerator;
            if (monitoring)
                monitoringWorker = new MonitoringWorker(this);
        }

        public bool ContainsKey(Key key) => data.ContainsKey(key);
        public bool ContainsKey(string key) => ContainsKey(GenerateKey(key));

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
                    Factory factory;
                    System.IO.Stream resource;
                    if (custom.TryGetValue(key, out factory))
                        returnVal = new Entry(this, key, (_, __) => factory(), monitor1, monitors);
                    else if (streamSource.TryOpenRead(key.key, out resource))
                        returnVal = new Entry(this, key, (_, k) => _LoadFromResource(k, resource), monitor1, monitors);
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

        public virtual void AddCustom(string key, Factory factory)
            => AddCustom(GenerateKey(key), factory);

        protected void AddCustom(Key key, Factory factory)
        {
            if (ContainsKey(key) || custom.ContainsKey(key))
                throw new InvalidOperationException($"{GetType().Name} already contains custom entry \"{key.key}\".");

            custom[key] = factory;
        }

        public virtual void RefreshContent()
        {
            lock (data)
            {
                DisposeItem(defaultValue);
                if (defaultGenerator != null)
                    defaultValue = defaultGenerator();

                foreach (SysCol.KeyValuePair<Key, Entry> pair in data)
                    // TODO: Is that really a good idea?
                    //       We've literally just reassigned the default value to a potentially new instance.
                    //       Plus: What if we're currently loading this in background and that's why it's default?
                    //       Shouldn't we then abort the current loading process and restart it?
                    //       Better than aborting is probably letting it finish and dispose the result.
                    if (!ReferenceEquals(pair.Value.content, defaultValue))
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

        DataType _LoadFromResource(Key key, System.IO.Stream resource)
        {
            if (!resource.CanRead)
                throw new Exception("Resource stream is unreadable. It was likely closed.");

            resource.Position = 0;
            DataType obj = LoadFromResource(key, resource);

            resource.Dispose();

            return obj;
        }

        protected abstract DataType LoadFromResource(Key key, System.IO.Stream resource);

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

        protected abstract void DisposeItem(DataType obj);

        protected override void DoDispose()
        {
            monitoringWorker?.Dispose();
            base.DoDispose();

            lock (data)
            {
                if (defaultValue != null)
                    DisposeItem(defaultValue);
                defaultValue = default(Entry);

                foreach (SysCol.KeyValuePair<Key, Entry> pair in data)
                    DisposeItem(pair.Value.content);

                data.Clear();
                custom.Clear();
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
