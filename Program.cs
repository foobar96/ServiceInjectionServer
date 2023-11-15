
using System.Reflection;



var builder = WebApplication.CreateBuilder(args);

// Additional AppSettings
builder.Configuration.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// builder.Services.AddScoped<SampleService>();
// builder.Services.AddSingleton<PeriodicHostedService>();
// builder.Services.AddSingleton<PeriodicHostedService>();
// builder.Services.AddHostedService(
//     provider => provider.GetRequiredService<PeriodicHostedService>());
// builder.Services.AddHostedService(
//     provider => provider.GetRequiredService<PeriodicHostedService>());

// builder.Services.AddHostedService<MyBackgroundService>();
// builder.Services.AddHostedService<MyBackgroundService>();






var app = builder.Build();

await BuildBackgroundServicesFromAppSettings();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// var service = new GreeterService("Api");
// var cancelToken = new CancellationToken();
// service.StartAsync(cancelToken);

// app.MapGet("/Greeter", () => {return service.GetType().ToString();} );

app.MapMethods($"/addservice", new[] { "PATCH" }, async (
                PeriodicHostedServiceState state
                ) =>
                {

                    Console.WriteLine("Hellohello");
                    if(state.NewService is not null)
                    {
                        Console.WriteLine("Letsbuildit");
                        // for(int i = 0; i < state.NewService.ConstructorArgument.Length; i++)
                        // {
                        //     state.NewService.ConstructorArgument[i] = state.NewService.ConstructorArgument[i].ToString(); // Actually necessary or Activator will not know that there are strings in the object[]
                        // }
                        await BuildServiceFromDescription(state.NewService);
                    }
                        
                });

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



async Task BuildBackgroundServicesFromAppSettings()
{

    var arr = builder.Configuration.GetSection("Services").Get<List<ServiceDescription>>();

    for(int i = 0; i < arr?.Count; i++)
    {
        
        try
        {
            (var typeInstance, var myType, var cancelToken) = await BuildServiceFromDescription(arr[i]);


            // Some mapping just for remote control and to justify trying this in a webapi project

            // Map the termination of the services
            // curl --location --request PATCH 'http://localhost:5048/service/0' --header 'Content-Type: application/json' --data-raw '{"RequestTermination": true}'
            app?.MapMethods($"/service/{i}", new[] { "PATCH" }, async (
                PeriodicHostedServiceState state
                ) =>
                {
                    if(state.RequestTermination)
                    {
                        MethodInfo? stopMethod = myType.GetMethod("StopAsync");
                        stopMethod?.Invoke(typeInstance, new object[] {cancelToken});
                    }
                        
                });

            // Map Service Info GET returns the assembly type
            // curl http://localhost:5048/service/0
            app?.MapGet($"/service/{i}", () => { MethodInfo? typeMethode = myType.GetMethod("GetType"); return typeMethode?.Invoke(typeInstance, Array.Empty<object>())?.ToString(); });

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

    Console.WriteLine(description.ConstructorArgument.GetType());
    Console.WriteLine(description.ConstructorArgument.Length);
    Console.WriteLine(description.ConstructorArgument[0]);
    Console.WriteLine(description.ConstructorArgument[0]?.GetType());
    
    object? typeInstance = Activator.CreateInstance(myType ?? throw new NullReferenceException("Type turned out to be null"), description.ConstructorArgument);
    //object typeInstance = Activator.CreateInstance(myType, new object[] {"Phrase", 3, 5});
    //object typeInstance = Activator.CreateInstance(myType, new object[] {"Phrase"});

    MethodInfo? startMethod = myType.GetMethod("StartAsync");

    var cancelToken = new CancellationToken();

    object? result = startMethod?.Invoke(typeInstance, new object[] {cancelToken});

    return (typeInstance, myType, cancelToken);
}

record PeriodicHostedServiceState(bool RequestTermination, ServiceDescription NewService);
