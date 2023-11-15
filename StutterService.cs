public class StutterService : BackgroundService
{
    private readonly string _phrase;
    private readonly int _stutterFaktor;
    private readonly int _interval;

    public StutterService(string phrase, string stutterFaktor, string interval)
    {
        this._phrase = phrase;
        this._stutterFaktor = int.TryParse(stutterFaktor, out _ ) ?  int.Parse(stutterFaktor) : 5;
        this._interval = int.TryParse(interval, out _ ) ?  int.Parse(interval) : 5;
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