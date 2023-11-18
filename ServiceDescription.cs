using System.Text.Json;

namespace settings_injection;

public class ServiceDescription
{
    public string TypeName { get; set; } = string.Empty;

    public string[] Parameters { get; set; } = Array.Empty<string>();

    public IHostedService CreateInstance()
    {
        if (string.IsNullOrWhiteSpace(TypeName))
            throw new ArgumentException("Type name missing");

        var serviceType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.DefinedTypes
                .Where(t => t.ImplementedInterfaces.Contains(typeof(IHostedService))))
            .FirstOrDefault(s => s.Name == TypeName);

        if (serviceType is null)
            throw new NullReferenceException("Unknown service type");

        var serviceInstance = Activator.CreateInstance(serviceType, GetParsedParameters()) as IHostedService
            ?? throw new Exception("Could not create service instance");

        return serviceInstance;
    }

    private object[] GetParsedParameters()
    {
        return Parameters.Select(p =>
        {
            var parts = p.Split(":", 2);
            var value = parts[0] switch
            {
                "string" => parts[1],
                "int" => int.Parse(parts[1]),
                "double" => double.Parse(parts[1]),
                "float" => float.Parse(parts[1]),
                "bool" => bool.Parse(parts[1]),
                _ => JsonSerializer.Deserialize(parts[1], Type.GetType(parts[0])!)!
            };            

            return value;
        }).ToArray();
    }
}