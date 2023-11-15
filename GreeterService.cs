public class GreeterService : BackgroundService
{
    private readonly string name;

    public GreeterService(string name)
    {
        this.name = name;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your logic here
            Console.WriteLine($"Hello {name}");

            // Adjust the interval as needed
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}