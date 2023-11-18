namespace settings_injection;

public class StutterService : BackgroundService
{
    private readonly string _phrase;
    private readonly int _stutterFaktor;
    private readonly int _intervalSeconds;

    public StutterService(string phrase, int stutterFaktor, int intervalSeconds)
    {
        _phrase = phrase;
        _stutterFaktor = stutterFaktor;
        _intervalSeconds = intervalSeconds;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {

            char firstChar = _phrase[0];
            string stutteredString = new string(firstChar, _stutterFaktor) + _phrase;
            Console.WriteLine(stutteredString);

            try
            {
                // Adjust the interval as needed
                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
    }
}