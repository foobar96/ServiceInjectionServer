namespace settings_injection;

public class ServiceIdProvider
{
    private int _currentId = -1;

    public int GetNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }
}