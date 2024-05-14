using System;
using System.Threading;
using System.Threading.Tasks;

namespace Worker
{
    public interface IWorker
    {
        Task<string> GetTokenForNewAllocationAsync(string clientName, Guid allocationId, TimeSpan? timeSpan, CancellationToken cancellationToken);
        Task AllocateAsync(string clientName, Guid allocationId, TimeSpan? timeSpan, CancellationToken cancellationToken);
    }
}
