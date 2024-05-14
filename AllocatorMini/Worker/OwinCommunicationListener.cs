using Microsoft.Owin.Host.HttpListener;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Worker
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly string appRoot;
        private readonly IOwinAppBuilder startupAppBuilder;
        private readonly StatelessServiceContext serviceContext;
        private readonly IWorker workerService;
        private string listeningAddress;
        private IDisposable serverHandle;

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startupAppBuilder, StatelessServiceContext serviceContext, IWorker worker)
        {
            this.appRoot = appRoot;
            this.startupAppBuilder = startupAppBuilder;
            this.serviceContext = serviceContext;
            this.workerService = worker;
        }

        // Cancel everything and stop immediately.
        public void Abort() 
        {
            this.Shutdown();
        }

        // Stop listening for requests, finish any in-flight requests and shutdown gracefully.
        public Task CloseAsync(CancellationToken cancellationToken) 
        {
            this.Shutdown();
            return Task.FromResult(true);
        }

        // Start the web server and start listening for requests.
        // Get the endpoint information and create the URL that the service will listen on.
        // Return value is the address on which the web server is listening on.
        public Task<string> OpenAsync(CancellationToken cancellationToken) 
        {
            ServiceEventSource.Current.Message("OpenAsync started");
            EndpointResourceDescription serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
            int port = serviceEndpoint.Port;

            // "http://+" used here is to make sure the web server is listening on all available addresses, including localhost, FQDN, and the machine IP.
            this.listeningAddress = String.Format(
                CultureInfo.InvariantCulture,
                "http://+:{0}/{1}",
                port,
                String.IsNullOrWhiteSpace(this.appRoot)
                    ? String.Empty
                    : this.appRoot.TrimEnd('/') + '/');

            ServiceEventSource.Current.Message("Listening address is {0}", listeningAddress);
            ServiceEventSource.Current.Message("IPAddressOrFQDN is {0}", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            // The startup (appbuilder) class that was passed in to the constructor is used by the web server to bootstrap the web app.
            try
            {
                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder =>
                {
                    var listener = (OwinHttpListener)appBuilder.Properties["Microsoft.Owin.Host.HttpListener.OwinHttpListener"];
                    listener.GetRequestProcessingLimits(out int maxAccepts, out int maxRequests);
                    ServiceEventSource.Current.Message(String.Format("MaxAccepts: {0}. MaxRequests: {1}.", maxAccepts, maxRequests));
                    this.startupAppBuilder.Configuration(appBuilder, this.workerService);
                });
                // this.serverHandle = WebApp.Start<Startup>(publishAddress);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Got exception: {0}.", ex.ToString());
            }

            string publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN); // Replace "+" with the IP/FQDN.
            ServiceEventSource.Current.Message("Listening on {0}", publishAddress);
            return Task.FromResult(publishAddress);
        }

        private void Shutdown()
        {
            if (this.serverHandle != null)
            {
                try
                {
                    this.serverHandle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}
