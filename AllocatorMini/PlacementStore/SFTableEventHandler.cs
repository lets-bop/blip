using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlacementStore
{
    /// <summary>
    /// The generic class handles Service Fabric callbacks for a persisted tables
    /// for TKey and TValue
    /// </summary>
    internal class SFTableEventHandler<TKey, TValue>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        private readonly string tableName;
        private readonly string context;
        private readonly Action<TKey, TValue> notifyAddOrUpdate;
        private readonly Action<IReadOnlyCollection<KeyValuePair<TKey, TValue>>> notifyRehydrate;
        private readonly Action<TKey> notifyRemove;
        private readonly Stopwatch stopwatch;

        public SFTableEventHandler(
            string context,
            Action<TKey, TValue> notifyAddOrUpdate,
            Action<IReadOnlyCollection<KeyValuePair<TKey, TValue>>> notifyRehydrate,
            Action<TKey> notifyRemove)
        {
            this.context = context;
            this.tableName = typeof(TValue).Name;
            this.notifyAddOrUpdate = notifyAddOrUpdate;
            this.notifyRehydrate = notifyRehydrate;
            this.notifyRemove = notifyRemove;
            this.stopwatch = new Stopwatch();
        }

        public void OnDictionaryChangedHandler(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
        {
            switch (e.Action)
            {
                case NotifyDictionaryChangedAction.Clear:
                    NotifyDictionaryClearEventArgs<TKey, TValue> clearEvent = e as NotifyDictionaryClearEventArgs<TKey, TValue>;
                    this.ProcessClearNotification(clearEvent);
                    return;

                case NotifyDictionaryChangedAction.Add:
                    NotifyDictionaryItemAddedEventArgs<TKey, TValue> addEvent = e as NotifyDictionaryItemAddedEventArgs<TKey, TValue>;
                    this.ProcessAddNotification(addEvent);
                    return;

                case NotifyDictionaryChangedAction.Update:
                    NotifyDictionaryItemUpdatedEventArgs<TKey, TValue> updateEvent = e as NotifyDictionaryItemUpdatedEventArgs<TKey, TValue>;
                    this.ProcessUpdateNotification(updateEvent);
                    return;

                case NotifyDictionaryChangedAction.Remove:
                    NotifyDictionaryItemRemovedEventArgs<TKey, TValue> removeEvent = e as NotifyDictionaryItemRemovedEventArgs<TKey, TValue>;
                    this.ProcessRemoveNotification(removeEvent);
                    return;

                case NotifyDictionaryChangedAction.Rebuild:
                    this.LogEvent($"OnDictionaryChangedHandler::NotifyDictionaryChangedAction.Rebuild");
                    throw new InvalidOperationException("Rebuild should not be invoked for OnDictionaryChangedHandler");
            }
        }

        private void ProcessRemoveNotification(NotifyDictionaryItemRemovedEventArgs<TKey, TValue> removeEvent)
        {
            this.LogEvent($"ProcessRemoveNotification:{removeEvent.Key}");
            this.stopwatch.Restart();
            this.notifyRemove(removeEvent.Key);
        }

        private void ProcessClearNotification(NotifyDictionaryClearEventArgs<TKey, TValue> clearEvent)
        {
            this.LogEvent($"ProcessClearNotification");
        }

        private void ProcessUpdateNotification(NotifyDictionaryItemUpdatedEventArgs<TKey, TValue> updateEvent)
        {
            this.LogEvent($"ProcessUpdateNotification:{updateEvent.Key}");
            this.stopwatch.Restart();
            this.notifyAddOrUpdate(updateEvent.Key, updateEvent.Value);
        }

        private void ProcessAddNotification(NotifyDictionaryItemAddedEventArgs<TKey, TValue> addEvent)
        {
            this.LogEvent($"ProcessAddNotification:{addEvent.Key}");
            this.stopwatch.Restart();
            this.notifyAddOrUpdate(addEvent.Key, addEvent.Value);
        }

        public async Task OnDictionaryRebuildNotificationHandlerAsync(
            IReliableDictionary<TKey, TValue> origin,
            NotifyDictionaryRebuildEventArgs<TKey, TValue> rebuildNotification)
        {
            this.LogEvent("OnDictionaryRebuildNotificationHandlerAsync");
            this.stopwatch.Restart();
            List<KeyValuePair<TKey, TValue>> rehydrateList = new List<KeyValuePair<TKey, TValue>>();
            IAsyncEnumerator<KeyValuePair<TKey, TValue>> enumerator = rebuildNotification.State.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync(CancellationToken.None))
            {
                rehydrateList.Add(new KeyValuePair<TKey, TValue>(enumerator.Current.Key, enumerator.Current.Value));
            }

            this.notifyRehydrate(rehydrateList);
            this.LogEvent($"OnDictionaryRebuildNotificationHandlerAsync:Count {rehydrateList.Count}");
        }

        private void LogEvent(string message)
        {
            ServiceEventSource.Current.Message("Context: {0}. TValue:{1}. Message: {2}.", this.context, this.tableName, message);
        }
    }
}
