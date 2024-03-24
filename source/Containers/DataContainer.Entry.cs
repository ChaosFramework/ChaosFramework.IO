using ChaosFramework.Collections;
using ChaosFramework.Core;

namespace ChaosFramework.IO.Containers
{
    public abstract partial class DataContainer<DataType>
    {
        public sealed class Entry
        {
            public delegate DataType LoadProcedure(DataContainer<DataType> container, Key key);

            public static Entry Mock(DataType content) => new Entry(null, content);

            readonly DataContainer<DataType> parent;
            public readonly Key key;
            public DataType content { get; private set; }

            internal readonly AdvancedLinkedList<Disposable> myMonitors;
            internal LoadProcedure loadProcedure;

            bool monitoring => myMonitors != null;

            Entry(Key key, DataType content)
            {
                this.key = key;
                SetContent(content);
            }

            internal Entry(
                DataContainer<DataType> parent,
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
                    SetContent(loadProcedure(parent, key));
                else
                {
                    SetContent(parent.defaultValue);
                    new System.Threading.Tasks.Task(LoadContent).Start();
                }
            }

            void LoadContent() => SetContent(loadProcedure(parent, key));

            public void RefreshContent()
            {
                if (parent != null)
                {
                    parent.DisposeItem(this);
                    Load();
                }
            }

            public void SetContent(DataType content) => this.content = content;

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

            public static implicit operator DataType(Entry obj)
                => obj == null ? default(DataType) : obj.content;

            public override string ToString()
                => $"{GetType().Name}.Entry({key?.key ?? "<null>"})";
        }
    }
}
