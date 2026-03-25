using Azure.Messaging.ServiceBus;
using EventSourcing.BuildingBlocks.Infrastructure.Extensions;
using EventSourcing.ProjectionWorkers.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "EventSourcing.ProjectionWorkers")
        .WriteTo.Console()
        .WriteTo.File("logs/projection-workers-.log", rollingInterval: RollingInterval.Day);
});

// Add infrastructure building blocks
builder.Services.AddCosmosDbReadModels(builder.Configuration);
builder.Services.AddServiceBusMessaging(builder.Configuration);

// Configure Service Bus client for receiving messages
builder.Services.AddSingleton<ServiceBusClient>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Service Bus connection string is required");

    return new ServiceBusClient(connectionString);
});

// Configure projection worker options
builder.Services.Configure<ProjectionWorkerOptions>(
    builder.Configuration.GetSection("ProjectionWorkers"));

// Register projection handlers
builder.Services.AddScoped<OrderProjectionHandler>();

// Register the projection worker service
builder.Services.AddHostedService<ProjectionWorkerService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCosmosDb(builder.Configuration.GetConnectionString("CosmosDB")!, name: "cosmosdb")
    .AddCheck("projection-workers", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

var host = builder.Build();

// Ensure Cosmos DB containers are available before starting
try
{
    await host.Services.EnsureCosmosDbContainersAsync();
    Log.Information("Cosmos DB containers verified");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to connect to Cosmos DB");
    return 1;
}

Log.Information("Projection Workers starting...");

await host.RunAsync();

return 0;
