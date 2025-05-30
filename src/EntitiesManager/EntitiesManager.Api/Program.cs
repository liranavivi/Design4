using EntitiesManager.Api.Configuration;
using EntitiesManager.Api.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Clear default logging providers - OpenTelemetry will handle logging
builder.Logging.ClearProviders();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration);

// Add OpenTelemetry
builder.Services.AddOpenTelemetryObservability(builder.Configuration);

// Add Health Checks
builder.Services.AddHttpClient<OpenTelemetryHealthCheck>();
builder.Services.AddHealthChecks()
    .AddMongoDb(builder.Configuration.GetConnectionString("MongoDB")!)
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}:{5672}/")
    .AddCheck<OpenTelemetryHealthCheck>("opentelemetry");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting EntitiesManager API");
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
}

// Make Program class accessible for testing
public partial class Program { }
