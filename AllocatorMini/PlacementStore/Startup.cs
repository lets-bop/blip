using Owin;
using System;
using System.Web.Http;
using PlacementStore.App_Start;
using CommonLibs;

namespace PlacementStore
{
    public class Startup : IOwinAppBuilder
    {
        public void Configuration(string address, IAppBuilder appBuilder, IPlacementStore placementStore)
        {
            try
            {
                HttpConfiguration config = new PlacementStoreSelfHostConfiguration(address, placementStore);
                config.MapHttpAttributeRoutes();
                config.MessageHandlers.Add(new OpenTelemetryActivityHandler(PlacementStoreActivitySource.Value)); // Add open telemetry handler
                FormatterConfig.ConfigureFormatters(config.Formatters);
                appBuilder.UseWebApi(config);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message("Exception in Configuration: {0}", ex.ToString());
            }

        }
    }
}
