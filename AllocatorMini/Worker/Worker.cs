using CommonLibs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Worker.Common;

namespace Worker
{
    public class Worker : IWorker
    {
        AllocatorWorkQueue<AllocatorWorkItem> sharedQueue = null;

        public Worker()
        {
            this.sharedQueue = new AllocatorWorkQueue<AllocatorWorkItem>();

            for (int i = 0; i < 4; i++)
            {
                AllocatorInstance allocatorInstance = new AllocatorInstance(i, this.sharedQueue);
                allocatorInstance.Start();
            }
        }

        public async Task<string> GetTokenForNewAllocationAsync(string clientName, Guid allocationId, TimeSpan? timeSpan, CancellationToken cancellationToken)
        {
            using (Activity activity = WorkerActivitySource.Value.StartActivity("Worker:GetToken"))
            {
                HttpClient client = new HttpClient();
                activity?.AddEvent(new ActivityEvent("Calling PlacementStore"));
                client.BaseAddress = new Uri("http://localhost/allocatorplacementstore/placementstore/gettoken/clientName/dummyClient/allocationId/90d8efb6-493a-4be2-9acd-682e9a768de7");
                client.DefaultRequestHeaders.Add(Header.TraceParent, activity?.Id.ToString());
                return await client.GetStringAsync("");
            }
        }

        public async Task AllocateAsync(string clientName, Guid allocationId, TimeSpan? timeSpan, CancellationToken cancellationToken)
        {
            using (Activity activity = WorkerActivitySource.Value.StartActivity("Worker:Allocate"))
            {
                await AllocateAction();
            }
        }

        private async Task AllocateAction()
        {
            AllocatorWorkItem<bool> allocatorWorkItem = new AllocatorWorkItem<bool>(0, this.RemotePlacementStoreCall);
            this.sharedQueue.TryEnqueue(allocatorWorkItem);
        }

        private bool RemotePlacementStoreCall()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost/allocatorplacementstore/placementstore/gettoken/clientName/dummyClient/allocationId/90d8efb6-493a-4be2-9acd-682e9a768de7");
            // client.DefaultRequestHeaders.Add(Header.TraceParent, activity?.Id.ToString());
            client.GetStringAsync("").Wait();
            return true;
        }
    }
}
