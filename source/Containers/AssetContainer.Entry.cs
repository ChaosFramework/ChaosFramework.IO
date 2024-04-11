using ChaosFramework.Collections;
using ChaosFramework.Core;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class AssetContainer<AssetType>
    {
        public sealed class Entry
        {
            object cancelLock = new object();
            CancellationToken mostRecentLoad = null;

            /// <summary>
            ///     A delegate representing the load procedure for <typeparamref name="AssetType"/>.
            ///     Can be cancelled with the provided <see cref="CancellationToken"/>.
            ///     If cancelled, this function must return either a <typeparamref name="AssetType"/>
            ///     that can safely be disposed with <see cref="AssetContainer{AssetType}.Dispose(Key)"/>
            ///     or <see langword="default"/>(<typeparamref name="AssetType"/>) in which case
            ///     <see cref="AssetContainer{AssetType}.Dispose(Key)"/> is not called.
            /// </summary>
            /// <param name="key"> The key to retrieve an asset for. </param>
            /// <param name="cancel">
            ///     The <see cref="CancellationToken"/> to be used for cancellation.
            ///     If <see langword="null"/> the <see cref="LoadProcedure"/> cannot be cancelled.
            /// </param>
            /// <returns>
            ///     A valid <typeparamref name="AssetType"/> if the load procedure was not cancelled.
            ///     <see langword="null"/> or a safely disposable <typeparamref name="AssetType"/> otherwise.
            /// </returns>
            public delegate AssetType LoadProcedure(Key key, CancellationToken cancel);

            public static Entry Mock(AssetType content) => new Entry(null, content);
            public static Entry Mock(LoadProcedure loadProcedure) => new Entry(null, loadProcedure);

            readonly AssetContainer<AssetType> parent;
            public readonly Key key;

            ChaosUtil.Primitives.Wrapper<AssetType> _content = null;
            public AssetType content => _content == null ? parent.defaultValue : _content.value;

            internal readonly AdvancedLinkedList<Disposable> myMonitors;
            internal LoadProcedure loadProcedure;

            bool monitoring => myMonitors != null;

            Entry(Key key, AssetType content)
            {
                this.key = key;
                _content = new ChaosUtil.Primitives.Wrapper<AssetType>(content);
            }

            Entry(Key key, LoadProcedure loadProcedure)
            {
                this.key = key;
                this.loadProcedure = loadProcedure;
                Load();
            }

            internal Entry(
                AssetContainer<AssetType> parent,
                Key key,
                LoadProcedure loadProcedure,
                Disposable monitor1,
                params Disposable[] monitors
                )
            {
                this.parent = parent;
                this.key = key;
                this.loadProcedure = loadProcedure;
                Load();

                if (parent.monitoringWorker != null)
                    myMonitors = new AdvancedLinkedList<Disposable>();

                AddMonitors(monitor1, monitors);
            }

            internal void Load()
            {
                if (parent == null || !parent.backgroundLoading)
                    _content = new ChaosUtil.Primitives.Wrapper<AssetType>(loadProcedure(key, null));
                else
                {
                    CancellationToken myCancellation = new CancellationToken();
                    lock (cancelLock)
                    {
                        _content = null;
                        mostRecentLoad?.Cancel();
                        mostRecentLoad = myCancellation;
                    }
                    new System.Threading.Tasks.Task(LoadContent, myCancellation).Start();
                }
            }

            void LoadContent(object state)
            {
                CancellationToken myCancellation = (CancellationToken)state;
                if (myCancellation.canceled)
                    return;

                AssetType value = loadProcedure(key, myCancellation);

                if (myCancellation.canceled)
                {
                    if (value != null)
                        parent.DisposeItem(value);
                }
                else
                    lock (cancelLock)
                        _content = new ChaosUtil.Primitives.Wrapper<AssetType>(value);
            }

            public void RefreshContent()
            {
                if (parent != null)
                {
                    parent.DisposeItem(this);
                    Load();
                }
            }

            public void AddMonitors(Disposable monitor1, params Disposable[] monitors)
            {
                if (monitor1 == null)
                    throw new System.ArgumentNullException(nameof(monitor1));

                if (monitoring)
                {
                    myMonitors.AddUnique(monitor1);

                    foreach (Disposable obj in monitors)
                        if (obj != null)
                            myMonitors.AddUnique(obj);
                }
            }

            public void RemoveMonitors(params Disposable[] monitors)
            {
                if (monitoring)
                    foreach (Disposable obj in monitors)
                        myMonitors.Remove(obj);
            }

            public static implicit operator AssetType(Entry obj)
                => obj == null ? default(AssetType) : obj.content;

            public override string ToString()
                => $"{GetType().Name}.Entry({key?.key ?? "<null>"})";
        }
    }
}
