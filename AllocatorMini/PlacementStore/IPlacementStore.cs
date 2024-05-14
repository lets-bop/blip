using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlacementStore
{
    public interface IPlacementStore
    {
        Task<string> GetTokenForNewAllocationAsync(string clientName, Guid allocationId, TimeSpan? timeSpan, CancellationToken cancellationToken);
        void TestGetAsyncEnumeratorOnReliableStateManager();
    }
}
