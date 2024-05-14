using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace PlacementStore
{
    public class PlacementStore : IPlacementStore
    {
        LongSFTransactionTimeFailover longSFTransactionTimeFailover;
        IReliableStateManager reliableStateManager;

        private readonly object lockObj;
        private readonly SFTableEventHandler<string, decimal> placementNodeKafkaEventHandler;
        private readonly SFTableEventHandler<string, double> suspendedNodeKafkaEventHandler;
        

        public PlacementStore(IReliableStateManager stateManager)
        {
            this.lockObj = new object();
            this.reliableStateManager = stateManager;
            this.longSFTransactionTimeFailover = new LongSFTransactionTimeFailover(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(300), 2000000, false, null);

            this.placementNodeKafkaEventHandler = new SFTableEventHandler<string, decimal>(
                "SFStateManagerPlacementNodeEventHandler",
                this.PlacementNodeAddOrUpdateAction,
                null,
                null);

            this.suspendedNodeKafkaEventHandler = new SFTableEventHandler<string, double>(
                "SFStateManagerPlacementNodeEventHandler",
                this.SuspendedNodeAddOrUpdateAction,
                null,
                null);
        }

        public async Task<string> GetTokenForNewAllocationAsync(string clientName, Guid allocationId, TimeSpan? timeSpan, CancellationToken cancellationToken)
        {
            //await this.AddToPlacementNodeDictionary(clientName);
            //await this.AddToSuspendedNodeDictionary(clientName);

            using (Activity activity = PlacementStoreActivitySource.Value.StartActivity("PlacementStore:GetToken"))
            {
                activity?.AddEvent(new ActivityEvent("PS: Begin token processing"));
                // await Task.Delay(1000);
                activity?.AddEvent(new ActivityEvent("PS: End token processing"));

                Stopwatch stopwatch = Stopwatch.StartNew();
                longSFTransactionTimeFailover.NotifyTransactionTime(1000);
                stopwatch.Stop();
                ServiceEventSource.Current.Message("NotifyTransactionTime took {0}", stopwatch.ElapsedTicks);
                return Guid.NewGuid().ToString();
            }
        }

        public void TestGetAsyncEnumeratorOnReliableStateManager()
        {
            try
            {
                ServiceEventSource.Current.Message("Inside TestGetAsyncEnumeratorOnReliableStateManager");
                IAsyncEnumerator<IReliableState> enumerator = this.reliableStateManager.GetAsyncEnumerator();
                ServiceEventSource.Current.Message("TestGetAsyncEnumeratorOnReliableStateManager complete!!");
            }
            catch (Exception ex) 
            {
                ServiceEventSource.Current.Message("Exception during rebuild " + ex.ToString());
            }
        }


        public async Task AddToPlacementNodeDictionary(string clientName)
        {
            try
            {
                var dic = await this.reliableStateManager.GetOrAddAsync<IReliableDictionary<string, decimal>>("placementNodeDictionary");
                ServiceEventSource.Current.Message("AddToDictionary: {0}", dic.Name);
                using (var tx = this.reliableStateManager.CreateTransaction())
                {
                    IAsyncEnumerator<IReliableState> enumerator = this.reliableStateManager.GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        if (enumerator.Current is IReliableDictionary<string, decimal> dictionary1)
                        {
                            (new WrappedCallback<string, decimal>(new[] { this.placementNodeKafkaEventHandler })).Register(dictionary1);
                            ServiceEventSource.Current.Message("AddToDictionary: placementNodeDictionary {0}", enumerator.Current.ToString());
                        }
                        else 
                        {
                            ServiceEventSource.Current.Message("AddToDictionary: OTHER Skipped {0}", enumerator.Current.ToString());
                        }
                    }

                    if (await dic.ContainsKeyAsync(tx, clientName))
                    {
                        await dic.TryUpdateAsync(tx, clientName, 0, 0);
                    }
                    else
                    {
                        await dic.AddAsync(tx, clientName, 0);
                    }

                    await tx.CommitAsync();
                    ServiceEventSource.Current.Message("AddToDictionary: Complete");
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("PlacementStore: Caught exception {0}", ex.ToString());
            }
        }

        public async Task AddToSuspendedNodeDictionary(string clientName)
        {
            try
            {
                var dic = await this.reliableStateManager.GetOrAddAsync<IReliableDictionary<string, double>>("suspendedNodeDictionary");
                ServiceEventSource.Current.Message("AddToDictionary: {0}", dic.Name);

                using (var tx = this.reliableStateManager.CreateTransaction())
                {
                    IAsyncEnumerator<IReliableState> enumerator = this.reliableStateManager.GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        if (enumerator.Current is IReliableDictionary<string, double> dictionary1)
                        {
                            (new WrappedCallback<string, double>(new[] { this.suspendedNodeKafkaEventHandler })).Register(dictionary1);
                            ServiceEventSource.Current.Message("AddToDictionary: suspendedNodeDictionary {0}", enumerator.Current.ToString());
                        }
                        else
                        {
                            ServiceEventSource.Current.Message("AddToDictionary: OTHER Skipped {0}", enumerator.Current.ToString());
                        }
                    }

                    if (await dic.ContainsKeyAsync(tx, clientName))
                    {
                        await dic.TryUpdateAsync(tx, clientName, 0, 0);
                    }
                    else
                    {
                        await dic.AddAsync(tx, clientName, 0);
                    }

                    await tx.CommitAsync();
                    ServiceEventSource.Current.Message("AddToDictionary: Complete");
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("PlacementStore: Caught exception {0}", ex.ToString());
            }
        }

        private void PlacementNodeAddOrUpdateAction(string key, decimal value)
        {
            lock (this.lockObj)
            {
                ServiceEventSource.Current.Message("PlacementStore: PlacementNodeAddOrUpdateAction. Key:{0}. Value:{1}", key, value);
            }
        }

        private void SuspendedNodeAddOrUpdateAction(string key, double value)
        {
            lock (this.lockObj)
            {
                ServiceEventSource.Current.Message("PlacementStore: SuspendedNodeAddOrUpdateAction. Key:{0}. Value:{1}", key, value);
            }
        }

        private class WrappedCallback<TKey, TValue>
            where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            private readonly SFTableEventHandler<TKey, TValue>[] sfTableEventHandlers;

            public WrappedCallback(SFTableEventHandler<TKey, TValue>[] sFTableEventHandlers)
            {
                this.sfTableEventHandlers = sFTableEventHandlers;
            }

            public void Register(IReliableDictionary<TKey, TValue> reliableDictionary)
            {
                foreach (var handler in this.sfTableEventHandlers)
                {
                    reliableDictionary.DictionaryChanged -= handler.OnDictionaryChangedHandler;
                    reliableDictionary.DictionaryChanged += handler.OnDictionaryChangedHandler;
                }

                reliableDictionary.RebuildNotificationAsyncCallback = this.WrappedDelegate;
            }

            public async Task WrappedDelegate(
                IReliableDictionary<TKey, TValue> origin,
                NotifyDictionaryRebuildEventArgs<TKey, TValue> rebuildNotification)
            {
                foreach (var handler in this.sfTableEventHandlers)
                {
                    await handler.OnDictionaryRebuildNotificationHandlerAsync(origin, rebuildNotification);
                }
            }
        }
    }
}
