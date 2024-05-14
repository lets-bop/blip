using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using CommonLibs;
using Microsoft.ServiceFabric.Services.Runtime;
using static System.Collections.Specialized.BitVector32;

namespace PlacementStore
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceEventSource.Current.Message("In placementstore main!!!.");

                ServiceRuntime.RegisterServiceAsync("PlacementStoreType",
                    context =>
                    {
                        IEnumerable<ConfigProperty> configs = GetConfigSettings("PlacementStoreSettings");
                        foreach (var config in configs) 
                        {
                            ServiceEventSource.Current.Message("Final Config-{0}:{1}", config.Name, config.Value);
                        }

                        return new PlacementStoreSFService(context);
                    }).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(PlacementStoreSFService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        public static IEnumerable<ConfigProperty> GetConfigSettings(string configSection)
        {
            ConfigurationPackage configurationPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");

            List<ConfigProperty> settingsList = new List<ConfigProperty>();
            foreach (ConfigurationSection section in configurationPackage.Settings.Sections)
            {
                ServiceEventSource.Current.Message("Section-{0}", section.Name);

                List<ConfigProperty> list = new List<ConfigProperty>();
                foreach (ConfigurationProperty parameter in section.Parameters)
                {
                    ServiceEventSource.Current.Message("Adding-{0}:{1} from section {2}.", parameter.Name, parameter.Value, section.Name);
                    list.Add(new ConfigProperty(parameter.Name, parameter.Value, section.Name));
                }
            }

            return settingsList;
        }
    }
}
