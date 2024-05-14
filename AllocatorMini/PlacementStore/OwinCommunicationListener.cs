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

namespace PlacementStore
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        private readonly string appRoot;
        private readonly IOwinAppBuilder startupAppBuilder;
        private readonly StatefulServiceContext serviceContext;
        private readonly IPlacementStore placementStore;
        private string listeningAddress;
        private IDisposable serverHandle;

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startupAppBuilder, StatefulServiceContext serviceContext, IPlacementStore placementStore)
        {
            this.appRoot = appRoot;
            this.startupAppBuilder = startupAppBuilder;
            this.serviceContext = serviceContext;
            this.placementStore = placementStore;
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
            ServiceEventSource.Current.Message("Placement store: OpenAsync started");
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

            ServiceEventSource.Current.Message("Placement store: Listening address is {0}", listeningAddress);
            ServiceEventSource.Current.Message("Placement store: IPAddressOrFQDN is {0}", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            string publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN); // Replace "+" with the IP/FQDN.
            ServiceEventSource.Current.Message("Placement store: Listening on {0}", publishAddress);

            // The startup (appbuilder) class that was passed in to the constructor is used by the web server to bootstrap the web app.
            try
            {
                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder =>
                {
                    foreach (var kv in appBuilder.Properties)
                    {
                        ServiceEventSource.Current.Message(String.Format("appbuilderProperty: {0}.", kv.Key));
                    }

                    var listener = (OwinHttpListener)appBuilder.Properties["Microsoft.Owin.Host.HttpListener.OwinHttpListener"];
                    listener.GetRequestProcessingLimits(out int maxAccepts, out int maxRequests);
                    ServiceEventSource.Current.Message(String.Format("MaxAccepts: {0}. MaxRequests: {1}.", maxAccepts, maxRequests));
                    listener.SetRequestQueueLimit(int.MaxValue);
                    listener.SetRequestProcessingLimits(maxAccepts, int.MaxValue);

                    this.placementStore.TestGetAsyncEnumeratorOnReliableStateManager();
                    this.startupAppBuilder.Configuration(publishAddress, appBuilder, this.placementStore);
                });

                port = 8082;
                this.listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    String.IsNullOrWhiteSpace(this.appRoot)
                        ? String.Empty
                        : this.appRoot.TrimEnd('/') + '/');

                //ServiceEventSource.Current.Message(String.Format("WebStart the second time with a different port address {0}!!!!", this.listeningAddress));

                //this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder =>
                //{
                //    foreach (var kv in appBuilder.Properties)
                //    {
                //        ServiceEventSource.Current.Message(String.Format("appbuilderProperty: {0}.", kv.Key));
                //    }

                //    var listener = (OwinHttpListener)appBuilder.Properties["Microsoft.Owin.Host.HttpListener.OwinHttpListener"];
                //    listener.GetRequestProcessingLimits(out int maxAccepts, out int maxRequests);
                //    ServiceEventSource.Current.Message(String.Format("MaxAccepts: {0}. MaxRequests: {1}.", maxAccepts, maxRequests));
                //    listener.SetRequestQueueLimit(int.MaxValue);
                //    listener.SetRequestProcessingLimits(maxAccepts, int.MaxValue);

                //    this.placementStore.TestGetAsyncEnumeratorOnReliableStateManager();
                //    this.startupAppBuilder.Configuration(publishAddress, appBuilder, this.placementStore);
                //});

                //ServiceEventSource.Current.Message(String.Format("WebStart the second time!!!! Completed"));

                // this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startupAppBuilder.Configuration(publishAddress, appBuilder, this.placementStore));
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Placement store: Got exception: {0}.", ex.ToString());
            }

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
