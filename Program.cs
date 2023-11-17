
using System.Linq.Expressions;
using System.Reflection;



var builder = WebApplication.CreateBuilder(args);

// Additional AppSettings
if(builder.Environment.IsEnvironment("Testing"))
    builder.Configuration.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await BuildBackgroundServicesFromAppSettings(app);

// Add Service examples:
// curl --location --request PATCH 'http://localhost:5048/service' --header 'Content-Type: application/json' --data-raw '{"NewService": {"AssemblyName": "GreeterService", "ConstructorArguments": [{"StringValue": "Remote"}]}}'
// curl --location --request PATCH 'http://localhost:5048/service' --header 'Content-Type: application/json' --data-raw '{"NewService": {"AssemblyName": "StutterService", "ConstructorArguments": [{"StringValue": "Hello"}, {"IntValue": 4}, {"IntValue": 1}]}}'
// Terminate Service example:
// curl --location --request PATCH 'http://localhost:5048/service' --header 'Content-Type: application/json' --data-raw '{"ServiceId": 0, "RequestTermination": true}'
// Get service info example:
// curl --location --request PATCH 'http://localhost:5048/service' --header 'Content-Type: application/json' --data-raw '{"ServiceId": 0}'
app.MapMethods($"/service", new[] { "PATCH" }, async (
                PeriodicHostedServiceState state
                ) =>
                {
                    return await HandlePatchServiceRequest(state);                       
                });

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



/// <summary>
/// Builds background services based on the configuration in appsettings.json.
/// </summary>
/// <param name="app">The WebApplication instance.</param>
async Task BuildBackgroundServicesFromAppSettings(WebApplication app)
{
    // Retrieve the list of service descriptions from the configuration
    var serviceArray = builder.Configuration.GetSection("Services").Get<List<ServiceDescription>>();

    if(serviceArray is null)
    {
        Console.WriteLine("Could not retrieve array of services. Check your appsettings.json format");
        return;
    }

    // Iterate through each service description and build the corresponding service
    foreach(var element in serviceArray)
    {
        
        try
        {
            // Build the service instance from the description
            (var typeInstance, var myType, var cancelToken) = await BuildServiceFromDescription(element);

            // Generate a unique ID for the service and add it to the service manager
            var id = ServiceIdProvider.Instance.GetNextId(); 
            ServiceManager.Instance.AddServiceRecord(id, new ServiceRecord { TypeInstance = typeInstance, MyType = myType, CancelToken = cancelToken });

        }
        catch(TypeLoadException e)
        {
            Console.WriteLine(e.Message);
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }

        // Add a delay to avoid CPU spikes when executing logic
        await Task.Delay(TimeSpan.FromSeconds(0.1321f));
    }
}

/// <summary>
/// Builds a service instance based on the provided ServiceDescription.
/// </summary>
/// <param name="description">The ServiceDescription containing the assembly name and constructor arguments.</param>
/// <returns>A tuple containing the service instance, the service type, and the cancellation token.</returns>
async Task<(object?, Type?, CancellationToken)> BuildServiceFromDescription(ServiceDescription description)
{
    // Check if the assembly name is provided
    if (description.AssemblyName == null)
        throw new NullReferenceException("Could not read Assembly Name");

    // Get the service type based on the assembly name
    Type? myType = Type.GetType(description.AssemblyName);
    var constructorArray = UnpackConstructorArray(description.ConstructorArguments ?? Array.Empty<ConstructorArrayItem>());

    // Check if the service type is found
    if (myType == null)
        throw new NullReferenceException("Type turned out to be null");

    // Create an instance of the service type and invoke the StartAsync method
    object? typeInstance = Activator.CreateInstance(myType, constructorArray);
    MethodInfo? startMethod = myType.GetMethod("StartAsync");

    // Create a cancellation token for the service
    var cancelToken = new CancellationToken();
    startMethod?.Invoke(typeInstance, new object[] {cancelToken});

    return (typeInstance, myType, cancelToken);
}

/// <summary>
/// Handles the PATCH service request by either creating a new service or communicating with an existing one.
/// </summary>
/// <param name="state">The PeriodicHostedServiceState containing the service ID, termination request, and new service description.</param>
/// <returns>A string indicating the result of the request.</returns>
async Task<string> HandlePatchServiceRequest(PeriodicHostedServiceState state)
{
    // We only want to create a new service
    if(state.NewService is not null)
    {
        // Build the new service and add it to the service manager
        var (typeInstance, myType, cancelToken) = await BuildServiceFromDescription(state.NewService);
        var id = ServiceIdProvider.Instance.GetNextId(); 
        ServiceManager.Instance.AddServiceRecord(id, new ServiceRecord { TypeInstance = typeInstance, MyType = myType, CancelToken = cancelToken });
        return $"Service created with Id: {id}";
    }

    // Or communicate with an existing one...
    if(state.ServiceId is null)
        return "Nothing happened";

    // Retrieve the requested service from the service manager
    var serviceRequested = ServiceManager.Instance.GetServiceRecord((int)state.ServiceId);

    // We only want info on the service
    if(!state.RequestTermination)
        return $"Service Type: {serviceRequested?.TypeInstance?.GetType()}";

    // Otherwise we want to terminate a service...
    MethodInfo? stopMethod = serviceRequested?.MyType?.GetMethod("StopAsync");
    stopMethod?.Invoke(serviceRequested?.TypeInstance, new object[] {serviceRequested?.CancelToken});

    return "Service terminated";
}

/// <summary>
/// Unpacks the constructor arguments from the ConstructorArrayItem array.
/// </summary>
/// <param name="input">The ConstructorArrayItem array.</param>
/// <returns>An object array containing the unpacked constructor arguments.</returns>
object?[] UnpackConstructorArray(ConstructorArrayItem[] input)
{
    // Unpack each constructor argument from the array
    return input.Select(elem =>
    {
        if (elem.StringValue != null)
            return (object)elem.StringValue;
        if (elem.IntValue != null)
            return (object)elem.IntValue;
        if (elem.FloatValue != null)
            return (object)elem.FloatValue;
        if (elem.BoolValue != null)
            return (object)elem.BoolValue;
        return null;
    }).Where(elem => elem != null).ToArray();
}



record PeriodicHostedServiceState(int? ServiceId, bool RequestTermination, ServiceDescription NewService);
