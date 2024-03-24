using ChaosFramework.Core;
using System;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class DataContainer<DataType> : Disposable, SysCol.IEnumerable<DataContainer<DataType>.Entry>
    {
        public delegate DataType Factory();

        static DataType LoadFromFile(DataContainer<DataType> container, Key key) => container.LoadFromFile(key);
        static DataType LoadFromArchive(DataContainer<DataType> container, Key key) => container.LoadFromArchive(key);

        public readonly ChaosArchive archive;
        public readonly bool backgroundLoading;

        readonly MonitoringWorker monitoringWorker;

        readonly SysCol.Dictionary<Key, Entry> data = new SysCol.Dictionary<Key, Entry>();
        readonly SysCol.Dictionary<Key, System.IO.Stream> resources = new SysCol.Dictionary<Key, System.IO.Stream>();
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
            ChaosArchive archive,
            bool monitoring,
            bool backgroundLoading = false,
            Factory defaultGenerator = null
            )
        {
            this.archive = archive;
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
            bool directoryExists = System.IO.Directory.Exists(directory);
            if (directoryExists)
                LoadDirectoryFromFileSystem(generateKey, directory, fileExtensions, recursive, monitor1, monitors);
            else if (archive == null)
                throw new Exception($"No directory \"{directory}\" found.");

            if (archive != null)
                foreach (string file in archive.GetFiles(fileExtensions, $"{directory}\\{(recursive ? "**" : "*")}"))
                    Load(generateKey(file), monitor1, monitors);
        }

        void LoadDirectoryFromFileSystem(
            Func<string, Key> generateKey,
            string directory,
            string[] fileExtensions,
            bool recursive,
            Disposable monitor1,
            params Disposable[] monitors
            )
        {
            if (recursive)
                foreach (string subDir in System.IO.Directory.EnumerateDirectories(directory))
                    LoadDirectoryFromFileSystem(generateKey, subDir, fileExtensions, recursive, monitor1, monitors);

            for (int i = 0; i < fileExtensions.Length; i++)
                fileExtensions[i] = fileExtensions[i].Trim().ToLower();

            foreach (string file in System.IO.Directory.EnumerateFiles(directory))
                foreach (string ext in fileExtensions)
                    if (file.ToLower().EndsWith(ext))
                    {
                        Load(generateKey(file), monitor1, monitors);
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
                    else if (resources.TryGetValue(key, out resource))
                        returnVal = new Entry(this, key, (_, k) => _LoadFromResource(k, resource), monitor1, monitors);
                    else if (System.IO.File.Exists(key.key))
                        returnVal = new Entry(this, key, LoadFromFile, monitor1, monitors);
                    else if (archive != null && archive.ContainsFile(key.key))
                        returnVal = new Entry(this, key, LoadFromArchive, monitor1, monitors);
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

        public void AddResource(string key, byte[] resource)
            => AddResource(key, new System.IO.MemoryStream(resource));

        public virtual void AddResource(string key, System.IO.Stream resource)
            => AddResource(GenerateKey(key), resource);

        protected void AddResource(Key key, System.IO.Stream resource)
        {
            if (ContainsKey(key) || resources.ContainsKey(key))
                throw new InvalidOperationException($"{GetType().Name} already contains resource \"{key.key}\".");

            resources[key] = resource;
        }

        public virtual void AddCustom(string key, Factory factory)
            => AddCustom(GenerateKey(key), factory);

        protected void AddCustom(Key key, Factory factory)
        {
            if (ContainsKey(key) || custom.ContainsKey(key))
                throw new InvalidOperationException($"{GetType().Name} already contains custom entry \"{key.key}\".");

            custom[key] = factory;
        }

        public void RefreshContent()
            => RefreshContent(false);

        public virtual void RefreshContent(bool overrideOnly)
        {
            lock (data)
            {
                DisposeItem(defaultValue);
                if (defaultGenerator != null)
                    defaultValue = defaultGenerator();

                if (overrideOnly && archive != null)
                {
                    foreach (SysCol.KeyValuePair<Key, Entry> pair in data)
                        if (archive.ContainsOverrideFile(pair.Key.key))
                            pair.Value.RefreshContent();
                }
                else
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

        protected abstract DataType LoadFromFile(Key key);

        DataType _LoadFromResource(Key key, System.IO.Stream resource)
        {
            if (!resource.CanRead)
                throw new Exception("Resource stream is unreadable. It was likely closed.");

            resource.Position = 0;
            return LoadFromResource(key, resource);
        }

        protected abstract DataType LoadFromResource(Key key, System.IO.Stream resource);

        internal DataType LoadFromArchive(Key key)
        {
            // TODO: REJOICE, NOBODY EVER DISPOSES THIS!
            System.IO.Stream archiveStream = archive.OpenRead(key.key);
            return _LoadFromResource(key, archiveStream);
        }

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
                resources.Clear();
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
