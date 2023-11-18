using Microsoft.Extensions.Options;

namespace settings_injection;

public class ServiceManager : IHostedService
{
    private readonly Dictionary<int, IHostedService> services = new();
    private readonly ServiceIdProvider idProvider;

    public ServiceManager(IOptions<ServiceManagerOptions> options, ServiceIdProvider idProvider)
    {
        this.idProvider = idProvider;

        foreach (var description in  options.Value.ServiceDescriptions)
        {
            var service = description.CreateInstance();
            AddService(service);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(services.Values.Select(s => s.StartAsync(cancellationToken)));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(services.Values.Select(s => s.StopAsync(cancellationToken)));
    }

    public async Task<int> HostServiceAsync(IHostedService service, CancellationToken cancellationToken = default)
    {
        await service.StartAsync(cancellationToken);

        var id = AddService(service);
        
        return id;
    }

    public IHostedService? GetService(int id)
    {
        services.TryGetValue(id, out IHostedService? service);
        return service;
    }

    private int AddService(IHostedService service)
    {
        var id = idProvider.GetNextId();
        services[id] = service;

        return id;
    }
}