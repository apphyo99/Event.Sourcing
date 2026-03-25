using EventSourcing.BuildingBlocks.Infrastructure.Extensions;
using EventSourcing.OutboxPublisher.Services;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "EventSourcing.OutboxPublisher")
        .WriteTo.Console()
        .WriteTo.File("logs/outbox-publisher-.log", rollingInterval: RollingInterval.Day);
});

// Add infrastructure building blocks
builder.Services.AddInfrastructureBuildingBlocks(builder.Configuration);

// Configure outbox publisher options
builder.Services.Configure<OutboxPublisherOptions>(
    builder.Configuration.GetSection("OutboxPublisher"));

// Register the outbox publisher service
builder.Services.AddHostedService<OutboxPublisherService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL")!, name: "postgresql")
    .AddCheck("outbox-publisher", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

var host = builder.Build();

// Ensure database is available before starting
try
{
    await host.Services.EnsureEventStoreDatabaseAsync();
    Log.Information("Event store database connection verified");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to connect to event store database");
    return 1;
}

Log.Information("Outbox Publisher Worker starting...");

await host.RunAsync();

return 0;
