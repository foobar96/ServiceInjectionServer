public class ServiceIdProvider
{
    private int _currentId = -1;

    private static readonly Lazy<ServiceIdProvider> _instance = new Lazy<ServiceIdProvider>(() => new ServiceIdProvider());

    public static ServiceIdProvider Instance => _instance.Value;

    private ServiceIdProvider() { }

    public int GetNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }
}