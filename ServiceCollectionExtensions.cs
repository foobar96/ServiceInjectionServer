namespace settings_injection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceManager(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ServiceManagerOptions>(config.GetSection(ServiceManagerOptions.SectionName));

        services.AddSingleton<ServiceIdProvider>();
        services.AddSingleton<ServiceManager>();

        services.AddHostedService(s => s.GetRequiredService<ServiceManager>());

        return services;
    }
}
