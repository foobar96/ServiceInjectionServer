using settings_injection;

var builder = WebApplication.CreateBuilder(args);

// Additional AppSettings
if (builder.Environment.IsEnvironment("Testing"))
    builder.Configuration.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServiceManager(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapServiceApi();

app.Run();
