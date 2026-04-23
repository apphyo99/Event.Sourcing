using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventSourcing.BuildingBlocks.Infrastructure.EventStore;

/// <summary>
/// Design-time factory for EF Core CLI (migrations). Does not require the API host or a live database.
/// </summary>
public sealed class EventStoreDbContextFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
    public EventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=eventsourcing_ef_design;Username=postgres;Password=postgres");
        return new EventStoreDbContext(optionsBuilder.Options);
    }
}
