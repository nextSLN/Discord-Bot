using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Threading;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Config.json")
            .Build();

        var bot = new Bot(config);
        
        // Create cancellation token source
        using var cts = new CancellationTokenSource();
        
        // Handle shutdown signal
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Shutting down...");
        };

        try
        {
            await bot.Start(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Clean shutdown
            await bot.Stop();
        }
    }
}
