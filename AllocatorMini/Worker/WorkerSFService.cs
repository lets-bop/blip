using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommonLibs;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using OpenTelemetry.Trace;

namespace Worker
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class WorkerSFService : StatelessService
    {
        private readonly TracerProvider openTelemetryTraceProvider;
        private readonly IWorker workerService;

        public WorkerSFService(StatelessServiceContext context)
            : base(context)
        { 
            this.openTelemetryTraceProvider = OpenTelemetryConfiguration.BuildTracerProvider(WorkerActivitySource.Value);
            this.workerService = new Worker();
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // return new ServiceInstanceListener[0];

            // The host (OwinCommunicationListener) is given an instance of the application WebAPI via StartUp.
            // Sample URL could look like: http://localhost/allocatorworker/api/values
            ServiceEventSource.Current.ServiceMessage(this.Context, "CreateServiceInstanceListeners started.");
            return new[]
            {
                new ServiceInstanceListener(initParams => new OwinCommunicationListener("allocatorworker", new Startup(), initParams, this.workerService))
            };
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        //protected override async Task RunAsync(CancellationToken cancellationToken)
        //{
        //    // TODO: Replace the following sample code with your own logic 
        //    //       or remove this RunAsync override if it's not needed in your service.

        //    long iterations = 0;

        //    while (true)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

        //        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        //    }
        //}
    }
}
