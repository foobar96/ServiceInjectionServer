namespace settings_injection;

public class ServiceManagerOptions
{
    public const string SectionName = "ServiceManager";

    public List<ServiceDescription> ServiceDescriptions { get; set; } = new();
}
