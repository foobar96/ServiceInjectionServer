
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

await BuildBackgroundServicesFromAppSettings(app);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


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



async Task BuildBackgroundServicesFromAppSettings(WebApplication app)
{

    var arr = builder.Configuration.GetSection("Services").Get<List<ServiceDescription>>();

    for(int i = 0; i < arr?.Count; i++)
    {
        
        try
        {
            (var typeInstance, var myType, var cancelToken) = await BuildServiceFromDescription(arr[i]);


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

        // Have some offset with your task creation to avoid cpu spikes when executing logic
        await Task.Delay(TimeSpan.FromSeconds(0.1321f));
    }
}

async Task<(object?, Type?, CancellationToken)> BuildServiceFromDescription(ServiceDescription description)
{
    // Get the type of a specified class.
    Type? myType = Type.GetType(description.AssemblyName ?? throw new NullReferenceException("Could not read Assembly Name"));

    var constructorArray = UnpackConstructorArray(description.ConstructorArguments ?? Array.Empty<ConstructorArrayItem>());

    object? typeInstance = Activator.CreateInstance(myType ?? throw new NullReferenceException("Type turned out to be null"), constructorArray);

    MethodInfo? startMethod = myType.GetMethod("StartAsync");

    var cancelToken = new CancellationToken();

    object? result = startMethod?.Invoke(typeInstance, new object[] {cancelToken});

    return (typeInstance, myType, cancelToken);
}

async Task<string> HandlePatchServiceRequest(PeriodicHostedServiceState state)
{
    if(state.NewService is not null)
    {
        (var typeInstance, var myType, var cancelToken) = await BuildServiceFromDescription(state.NewService);
        var id = ServiceIdProvider.Instance.GetNextId(); 
        ServiceManager.Instance.AddServiceRecord(id, new ServiceRecord { TypeInstance = typeInstance, MyType = myType, CancelToken = cancelToken });
        return $"Service created with Id: {id}";
    }

    if(state.ServiceId is not null)
    {
        var serviceRequested = ServiceManager.Instance.GetServiceRecord((int)state.ServiceId);

        if(state.RequestTermination)
        {
            MethodInfo? stopMethod = serviceRequested?.MyType?.GetMethod("StopAsync");
            stopMethod?.Invoke(serviceRequested?.TypeInstance, new object[] {serviceRequested?.CancelToken});

            return "Service terminated";
        }
        else
        {
            return $"Service Type: {serviceRequested?.TypeInstance?.GetType()}";
        }

    }

    return "Nothing happened";
}

object[] UnpackConstructorArray(ConstructorArrayItem[] input)
{
    var result = new List<object>();

    foreach(var elem in input)
    {
        if (elem.StringValue != null)
        {
            result.Add((string)elem.StringValue);
        }
        if (elem.IntValue != null)
        {
            result.Add((int)elem.IntValue);
        }
        if (elem.FloatValue != null)
        {
            result.Add((float)elem.FloatValue);
        }
        if (elem.BoolValue != null)
        {
            result.Add((bool)elem.BoolValue);
        }
    }

    return result.ToArray();
}



record PeriodicHostedServiceState(int? ServiceId, bool RequestTermination, ServiceDescription NewService);
