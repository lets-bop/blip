using Owin;
using System;
using System.Web.Http;
using Worker.App_Start;
using CommonLibs;

namespace Worker
{
    public class Startup : IOwinAppBuilder
    {
        public object MyContext;

        public void Configuration(IAppBuilder appBuilder, IWorker worker)
        {
            try
            {
                HttpConfiguration config = new WorkerSelfHostConfiguration(worker);
                config.MapHttpAttributeRoutes();
                config.MessageHandlers.Add(new OpenTelemetryActivityHandler(WorkerActivitySource.Value)); // Add open telemetry handler
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
