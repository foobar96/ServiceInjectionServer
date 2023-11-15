using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MyBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your logic here
            Console.WriteLine("MyBackgroundService");

            // Adjust the interval as needed
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}