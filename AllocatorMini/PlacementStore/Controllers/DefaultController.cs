using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace PlacementStore.Controllers
{
    [RoutePrefix("placementstore")]
    public class DefaultController : ConfigProvidingController<PlacementStoreSelfHostConfiguration>
    {
        [Route("GetToken/clientName/{clientName}/allocationId/{allocationId}")] // GET placementstore/gettoken
        public async Task<string> GetToken(string clientName, Guid allocationId)
        {
            // Thread.Sleep(5000);
            ServiceEventSource.Current.Message("Get called at {0}", DateTime.UtcNow);
            return await this.GetConfiguration().PlacementStoreService.GetTokenForNewAllocationAsync(clientName, allocationId, null, CancellationToken.None);
        }
    }
}
