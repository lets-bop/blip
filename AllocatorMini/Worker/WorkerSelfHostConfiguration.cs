using System.Web.Http;

namespace Worker
{
    public class WorkerSelfHostConfiguration : HttpConfiguration
    {
        internal IWorker WorkerService;

        public WorkerSelfHostConfiguration(IWorker worker)
        {
            this.WorkerService = worker;
        }
    }
}
