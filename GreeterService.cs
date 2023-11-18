namespace settings_injection;

public class GreeterService : BackgroundService
{
    private readonly string name;

    public GreeterService(string name)
    {
        this.name = name;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Your logic here
            Console.WriteLine($"Hello {name}");

            try
            {
                // Adjust the interval as needed
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
    }
}