using ChaosFramework.Collections;
using ChaosFramework.Core;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.IO.Containers
{
    public partial class AssetContainer<AssetType>
    {
        class MonitoringWorker : Disposable
        {
            const int COLLECTION_INTERVAL = 5000;

            readonly AssetContainer<AssetType> parent;

            readonly LinkedList<Key> disposals = new LinkedList<Key>();
            readonly System.ComponentModel.BackgroundWorker worker;
            readonly System.Threading.ManualResetEvent workerThreadDone;
            readonly System.Threading.ManualResetEvent workerInterruptRequested;

            public MonitoringWorker(AssetContainer<AssetType> parent)
            {
                this.parent = parent;

                workerThreadDone = new System.Threading.ManualResetEvent(false);
                workerInterruptRequested = new System.Threading.ManualResetEvent(false);
                worker = new System.ComponentModel.BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.ProgressChanged += ProcessDisposals;
                worker.DoWork += MonitoringLoop;
                worker.RunWorkerAsync();
            }

            void MonitoringLoop(object sender, System.ComponentModel.DoWorkEventArgs e)
            {
                while (alive)
                {
                    if (alive) workerInterruptRequested.WaitOne(COLLECTION_INTERVAL);
                    if (alive) MonitorDisposals();
                }

                workerThreadDone.Set();
            }

            void MonitorDisposals()
            {
                LinkedList<Key> tmpDisposals = new LinkedList<Key>();

                lock (parent.entries)
                    foreach (SysCol.KeyValuePair<Key, Entry> dataPair in parent.entries)
                    {
                        foreach (Disposable monitor in dataPair.Value.monitors)
                            if (monitor.disposed)
                                dataPair.Value.monitors.RemoveCurrent();

                        if (dataPair.Value.monitors.empty)
                            tmpDisposals.Add(dataPair.Key);
                    }

                lock (disposals)
                {
                    if (alive)
                    {
                        disposals.Add(tmpDisposals);
                        worker.ReportProgress(0);
                    }
                }
            }

            void ProcessDisposals(object sender, System.ComponentModel.ProgressChangedEventArgs args)
            {
                lock (disposals)
                {
                    foreach (Key key in disposals)
                    {
                        Entry entry;
                        if (!parent.entries.TryGetValue(key, out entry) || !entry.monitors.empty)
                            continue;

                        System.Diagnostics.Debug.WriteLine($"{parent.GetType().Name}: disposing \"{key.key}\"");
                        parent.RemoveEntry(key);
                    }
                    disposals.Clear();
                }
            }

            protected override void DoDispose()
            {
                base.DoDispose();
                workerInterruptRequested.Set();
                workerThreadDone.WaitOne();
                ProcessDisposals(null, null);
            }
        }
    }
}
