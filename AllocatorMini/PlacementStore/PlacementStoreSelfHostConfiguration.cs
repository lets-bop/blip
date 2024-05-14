using System.Web.Http;
using System.Web.Http.SelfHost;

namespace PlacementStore
{
    public class PlacementStoreSelfHostConfiguration : HttpSelfHostConfiguration
    {
        internal IPlacementStore PlacementStoreService;

        public PlacementStoreSelfHostConfiguration (string publishAddress, IPlacementStore placementStore) : base(publishAddress)
        {
            this.PlacementStoreService = placementStore;
            this.MaxConcurrentRequests = 1;
            this.SendTimeout = new System.TimeSpan(0, 0, 1);
        }
    }
}