using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Management.ServiceModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommonLibs;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Trace;

namespace PlacementStore
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class PlacementStoreSFService : StatefulService, IDisposable
    {
        private readonly TracerProvider openTelemetryTraceProvider;
        private PlacementStore placementStore;
        private readonly IReliableStateManager reliableStateManager;

        public PlacementStoreSFService(StatefulServiceContext context)
            : this(context, new ReliableStateManager(context))
        {
        }

        public PlacementStoreSFService(StatefulServiceContext context, ReliableStateManager reliableStateManager)
            : base(context, reliableStateManager)
        {
            ServiceEventSource.Current.Message("Constructing {0}", "PlacementStoreSFService");
            this.StateManager.StateManagerChanged += this.OnStateManagerChangedHandler;
            this.reliableStateManager = reliableStateManager;

            this.openTelemetryTraceProvider = OpenTelemetryConfiguration.BuildTracerProvider(PlacementStoreActivitySource.Value);
            this.placementStore = new PlacementStore(reliableStateManager);
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            // return new ServiceReplicaListener[0];

            // The host (OwinCommunicationListener) is given an instance of the application WebAPI via StartUp.
            // Sample URL could look like: http://localhost/allocatorplacementstore/api/values
            ServiceEventSource.Current.ServiceMessage(this.Context, "PlacementStoreSFService:: CreateServiceInstanceListeners started.");
            return new[]
            {
                new ServiceReplicaListener(initParams => new OwinCommunicationListener("allocatorplacementstore", new Startup(), initParams, this.placementStore))
            };
        }

        public void Dispose()
        {
            ServiceEventSource.Current.Message("PlacementStoreSFService::Dispose.");
            ServiceEventSource.Current.ServiceMessage(this.Context, "PlacementStoreSFService::Dispose.");
        }

        private void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e)
        {
            ServiceEventSource.Current.Message("OnStateManagerChangedHandler {0}", e.ToString());

            NotifyStateManagerRebuildEventArgs operation = e as NotifyStateManagerRebuildEventArgs;

            if (operation != null) 
            {
                IAsyncEnumerator<IReliableState> enumerator = operation.ReliableStates.GetAsyncEnumerator();
                this.ProcessStateManagerRebuildNotificationAsync(enumerator).Wait();
            }
            else
            {
                NotifyStateManagerSingleEntityChangedEventArgs op = e as NotifyStateManagerSingleEntityChangedEventArgs;
                ServiceEventSource.Current.Message("OnStateManagerChangedHandler {0}. {1}", op.Action.ToString(), op.ReliableState.ToString());
            }

            //if (e.Action == NotifyStateManagerChangedAction.Rebuild)
            //{
            //    return;
            //}
        }

        private async Task ProcessStateManagerRebuildNotificationAsync(IAsyncEnumerator<IReliableState> enumerator)
        {
            ServiceEventSource.Current.Message("ProcessStateManagerRebuildNotificationAsync");
            while (await enumerator.MoveNextAsync(CancellationToken.None))
            {
                ServiceEventSource.Current.Message("ProcessStateManagerRebuildNotificationAsync: {0}", enumerator.Current.ToString());
            }
        }

        ///// <summary>
        ///// This is the main entry point for your service replica.
        ///// This method executes when this replica of your service becomes primary and has write status.
        ///// </summary>
        ///// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            ServiceEventSource.Current.Message("PlacementStoreSFService: RunAsync started.");

            await this.placementStore.AddToPlacementNodeDictionary("client1");
            await this.placementStore.AddToSuspendedNodeDictionary("client1");

            try
            {
                IAsyncEnumerator<IReliableState> enumerator = this.reliableStateManager.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    await this.RebuildDictionaryAsync("RunAsync", this.reliableStateManager, enumerator.Current, cancellationToken);
                }

                ServiceEventSource.Current.Message("DONE WITH REBUILD");

                enumerator = this.reliableStateManager.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    ServiceEventSource.Current.Message("Enumerator Current:" + enumerator.Current.ToString());
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Exception during rebuild " + ex.ToString());
            }

            //using (var tx = this.reliableStateManager.CreateTransaction())
            //{
            //    while (await enumerator.MoveNextAsync(CancellationToken.None))
            //    {
            //        if (enumerator.Current is IReliableDictionary<string, decimal> dictionary1)
            //        {
            //            var enumerableTask = await dictionary1.CreateEnumerableAsync(tx);
            //            IAsyncEnumerator<KeyValuePair<string, decimal>> enumerator1 = enumerableTask.GetAsyncEnumerator();
            //            while (await enumerator1.MoveNextAsync(cancellationToken))
            //            {
            //                ServiceEventSource.Current.Message("RunAsync: placementNodeDictionary key:{0}, value:{1}", enumerator1.Current.Key.ToString(), enumerator1.Current.Value.ToString());
            //            }
            //        }
            //        else if (enumerator.Current is IReliableDictionary<string, double> dictionary2)
            //        {
            //            var enumerableTask = await dictionary2.CreateEnumerableAsync(tx);
            //            IAsyncEnumerator<KeyValuePair<string, double>> enumerator1 = enumerableTask.GetAsyncEnumerator();
            //            while (await enumerator1.MoveNextAsync(cancellationToken))
            //            {
            //                ServiceEventSource.Current.Message("RunAsync: suspendedNodeDictionary key:{0}, value:{1}", enumerator1.Current.Key.ToString(), enumerator1.Current.Value.ToString());
            //            }
            //        }
            //    }
            //}

                //var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

                //while (true)
                //{
                //    cancellationToken.ThrowIfCancellationRequested();

                //    using (var tx = this.StateManager.CreateTransaction())
                //    {
                //        var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                //        ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                //            result.HasValue ? result.Value.ToString() : "Value does not exist.");

                //        await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                //        // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                //        // discarded, and nothing is saved to the secondary replicas.
                //        await tx.CommitAsync();
                //    }

                //    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                //}
        }


        private async Task RebuildDictionaryAsync(string context, IReliableStateManager stateManager, IReliableState reliableState, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("In RebuildDictionaryAsync");

            // Note: We will purposely not be awaiting the rebuild dictionary operation invoked on the handler as it can be time consuming
            // and is in the hot-path of starting the primary/service. The task returned by the async operation will be hot/started.
            if (reliableState is IReliableDictionary<string, decimal> dictionary1)
            {
                using (var tx = stateManager.CreateTransaction())
                {
                    var enumerableTask = await dictionary1.CreateEnumerableAsync(tx);
                    await this.RebuildDictionaryAsync("RebuildDictionaryAsync", enumerableTask.GetAsyncEnumerator(), cancellationToken);
                }
            }
            else if (reliableState is IReliableDictionary<string, double> dictionary2)
            {
                using (var tx = stateManager.CreateTransaction())
                {
                    var enumerableTask = await dictionary2.CreateEnumerableAsync(tx);
                    await this.RebuildDictionaryAsync("RebuildDictionaryAsync", enumerableTask.GetAsyncEnumerator(), cancellationToken);
                }
            }
        }

        private async Task RebuildDictionaryAsync(string context, IAsyncEnumerator<KeyValuePair<string, decimal>> enumerator, CancellationToken cancellationToken)
        {
            while (await enumerator.MoveNextAsync(cancellationToken))
            {
                await Task.Delay(100);
                ServiceEventSource.Current.Message("RunAsync: placementNodeDictionary key:{0}, value:{1}", enumerator.Current.Key.ToString(), enumerator.Current.Value.ToString());
                await Task.Delay(100);
            }
        }

        private async Task RebuildDictionaryAsync(string context, IAsyncEnumerator<KeyValuePair<string, double>> enumerator, CancellationToken cancellationToken)
        {
            while (await enumerator.MoveNextAsync(cancellationToken))
            {
                ServiceEventSource.Current.Message("RunAsync: suspendedNodeDictionary key:{0}, value:{1}", enumerator.Current.Key.ToString(), enumerator.Current.Value.ToString());
            }
        }
    }
}
