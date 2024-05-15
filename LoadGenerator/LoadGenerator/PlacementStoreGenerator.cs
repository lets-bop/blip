using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public class PlacementStoreGenerator
    {
        static HttpClient client = new HttpClient();

        Task justWaitTask;

        public PlacementStoreGenerator()
        {
            client.BaseAddress = new Uri("http://localhost/allocatorplacementstore/placementstore/");
            this.justWaitTask = this.JustWait();
        }
        
        public async Task StartAsync(int count = 1)
        {
            Console.WriteLine("Awaiting for the task to complete");
            // await this.justWaitTask;

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(this.MakeCall("client1", Guid.NewGuid()));
            }

            int c = 0;
            foreach (var task in tasks)
            {
                Console.WriteLine("Waiting for tasks: {0}", c++);
                task.Wait();
                // Console.WriteLine(task.Result);
                task.Dispose();
            }
        }

        private async Task JustWait()
        {
            await Task.Delay(5000);
        }

        private async Task<string> MakeCall(string clientName, Guid allocId)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                HttpResponseMessage response = await client.GetAsync(
                    $"gettoken/clientName/{clientName}/allocationId/{allocId}");
                response.EnsureSuccessStatusCode();

                // Deserialize the updated product from the response body.
                string result = await response.Content.ReadAsStringAsync();

                Console.WriteLine(sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                return ex.GetType().FullName;
            }
        }
    }
}
