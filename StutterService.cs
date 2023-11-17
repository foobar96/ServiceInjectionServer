public class StutterService : BackgroundService
{
    private readonly string _phrase;
    private readonly int _stutterFaktor;
    private readonly int _interval;

    public StutterService(string phrase, int stutterFaktor, int interval)
    {
        this._phrase = phrase;
        this._stutterFaktor = stutterFaktor;
        this._interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            char firstChar = _phrase[0];
            string stutteredString = new string(firstChar, _stutterFaktor) + _phrase;
            Console.WriteLine(stutteredString);

            // Adjust the interval as needed
            await Task.Delay(TimeSpan.FromSeconds(_interval), stoppingToken);
        }
    }
}