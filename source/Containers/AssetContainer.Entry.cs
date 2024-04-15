using ChaosFramework.Collections;
using ChaosFramework.Core;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class AssetContainer<AssetType>
    {
        public sealed class Entry
        {
            internal class LoadKillPair
            {
                public readonly LoadProcedure load;
                public readonly KillProcedure kill;

                public LoadKillPair(LoadProcedure load, KillProcedure kill)
                {
                    if ((this.load = load) == null) throw new System.ArgumentNullException(nameof(load));
                    if ((this.kill = kill) == null) throw new System.ArgumentNullException(nameof(kill));
                }
            }

            object contentLock = new object();
            CancellationToken mostRecentLoad = null;

            /// <summary>
            ///     A delegate representing the load procedure for <typeparamref name="AssetType"/>.
            ///     Can be canceled with the provided <see cref="CancellationToken"/>.
            ///     If canceled, this function must return either a <typeparamref name="AssetType"/>
            ///     that can safely be disposed with <see cref="AssetContainer{AssetType}.DisposeItem(AssetType)"/>
            ///     or <see langword="default"/>(<typeparamref name="AssetType"/>) in which case
            ///     <see cref="AssetContainer{AssetType}.DisposeItem(AssetType)"/> is not called.
            /// </summary>
            /// <param name="key"> The key to retrieve an asset for. </param>
            /// <param name="cancel">
            ///     The <see cref="CancellationToken"/> to be used for cancellation.
            ///     If <see langword="null"/> the <see cref="LoadProcedure"/> cannot be canceled.
            /// </param>
            /// <returns>
            ///     A valid <typeparamref name="AssetType"/> if the load procedure was not canceled.
            ///     <see langword="null"/> or a safely disposable <typeparamref name="AssetType"/> otherwise.
            /// </returns>
            public delegate AssetType LoadProcedure(Key key, CancellationToken cancel);

            public delegate void KillProcedure(AssetType content);

            public static Entry Mock(LoadProcedure load, KillProcedure kill) => new Entry(new LoadKillPair(load, kill));

            readonly AssetContainer<AssetType> parent;
            public readonly Key key;

            ChaosUtil.Primitives.Wrapper<AssetType> _content = null;
            public AssetType content => _content == null ? parent.defaultValue : _content.value;

            internal readonly AdvancedLinkedList<Disposable> monitors;
            readonly LoadKillPair loadKill;

            bool monitoring => monitors != null;

            Entry(LoadKillPair loadKill)
            {
                this.loadKill = loadKill;
                Load();
            }

            internal Entry(
                AssetContainer<AssetType> parent,
                Key key,
                LoadKillPair loadKill,
                Disposable monitor1,
                params Disposable[] monitors
                )
            {
                this.parent = parent;
                this.key = key;
                this.loadKill = loadKill;
                Load();

                if (parent.monitoringWorker != null)
                    this.monitors = new AdvancedLinkedList<Disposable>();

                AddMonitors(monitor1, monitors);
            }

            internal void Load()
            {
                if (parent == null || !parent.backgroundLoading)
                    _content = new ChaosUtil.Primitives.Wrapper<AssetType>(loadKill.load(key, null));
                else
                {
                    CancellationToken cancel = new CancellationToken();
                    lock (contentLock)
                    {
                        _content = null;
                        mostRecentLoad?.Cancel();
                        mostRecentLoad = cancel;
                    }
                    new System.Threading.Tasks.Task(LoadContent, cancel).Start();
                }
            }

            void LoadContent(object state)
            {
                CancellationToken cancel = (CancellationToken)state;
                if (cancel.canceled)
                    return;

                AssetType value = loadKill.load(key, cancel);

                if (cancel.canceled)
                {
                    if (value != null)
                        loadKill.kill(value);
                }
                else
                    lock (contentLock)
                        _content = new ChaosUtil.Primitives.Wrapper<AssetType>(value);
            }

            internal void DisposeContent()
            {
                lock (contentLock)
                {
                    if (_content != null)
                    {
                        loadKill.kill(_content);
                        _content = null;
                    }
                }
            }

            public void RefreshContent()
            {
                DisposeContent();
                Load();
            }

            public void AddMonitors(Disposable monitor1, params Disposable[] monitors)
            {
                if (monitor1 == null)
                    throw new System.ArgumentNullException(nameof(monitor1));

                if (monitoring)
                {
                    this.monitors.AddUnique(monitor1);

                    foreach (Disposable obj in monitors)
                        if (obj != null)
                            this.monitors.AddUnique(obj);
                }
            }

            public void RemoveMonitors(params Disposable[] monitors)
            {
                if (monitoring)
                    foreach (Disposable obj in monitors)
                        this.monitors.Remove(obj);
            }

            public static implicit operator AssetType(Entry obj)
                => obj == null ? default(AssetType) : obj.content;

            public override string ToString()
                => $"{GetType().Name}.Entry({key?.key ?? "<null>"})";
        }
    }
}
