public class ServiceManager
{
    private static ServiceManager? instance = null;
    private static readonly object padlock = new object();

    private Dictionary<int, ServiceRecord> serviceRecords = new Dictionary<int, ServiceRecord>();

    ServiceManager()
    {
    }

    public static ServiceManager Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new ServiceManager();
                }
                return instance;
            }
        }
    }

    public void AddServiceRecord(int key, ServiceRecord record)
    {
        serviceRecords[key] = record;
    }

    public ServiceRecord? GetServiceRecord(int key)
    {
        serviceRecords.TryGetValue(key, out ServiceRecord? record);
        return record;
    }
}