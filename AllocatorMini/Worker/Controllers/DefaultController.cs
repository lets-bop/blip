using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Worker.Controllers
{
    [RoutePrefix("worker")]
    public class DefaultController : ConfigProvidingController<WorkerSelfHostConfiguration>
    {
        [Route("gettoken")] // GET worker/gettoken
        public async Task<string> GetToken()
        {
            ServiceEventSource.Current.Message("GetToken called at {0}", DateTime.UtcNow);
            return await this.GetConfiguration().WorkerService.GetTokenForNewAllocationAsync("dummyClient", Guid.Empty, null, CancellationToken.None);
        }

        [Route("Allocate/clientName/{clientName}/allocationId/{allocationId}")]
        public async Task Allocate(string clientName, Guid allocationId)
        {
            ServiceEventSource.Current.Message("Worker: Allocate called at {0}", DateTime.UtcNow);
            await this.GetConfiguration().WorkerService.AllocateAsync(clientName, allocationId, null, CancellationToken.None);
        }
    }
}
