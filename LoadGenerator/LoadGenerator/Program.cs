// See https://aka.ms/new-console-template for more information
using LoadGenerator;

internal class Program
{
    // private static async Task Main(string[] args)
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        PlacementStoreGenerator placementStoreGenerator = new PlacementStoreGenerator();
        placementStoreGenerator.StartAsync(100).Wait();
    }
}