using EventSourcing.BuildingBlocks.Domain.Events;

namespace EventSourcing.BuildingBlocks.Domain.Aggregates;

/// <summary>
/// Base class for aggregate roots in event sourcing
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _uncommittedEvents = new();
    private int _version = 0;

    /// <summary>
    /// Current version of the aggregate (number of events applied)
    /// </summary>
    public int Version => _version;

    /// <summary>
    /// Stream identifier for this aggregate
    /// </summary>
    public abstract string StreamId { get; }

    /// <summary>
    /// Stream type for this aggregate
    /// </summary>
    public abstract string StreamType { get; }

    /// <summary>
    /// Gets all uncommitted events for this aggregate
    /// </summary>
    /// <returns>Collection of uncommitted events</returns>
    public IReadOnlyCollection<DomainEvent> GetUncommittedEvents()
    {
        return _uncommittedEvents.AsReadOnly();
    }

    /// <summary>
    /// Marks all uncommitted events as committed
    /// </summary>
    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// Applies an event to this aggregate and adds it to uncommitted events
    /// </summary>
    /// <param name="domainEvent">The event to apply</param>
    protected void ApplyEvent(DomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        // Set the version for this event
        var nextVersion = _version + 1;
        domainEvent.SetVersion(nextVersion);

        // Apply the event to update aggregate state
        ApplyEventInternal(domainEvent);

        // Add to uncommitted events
        _uncommittedEvents.Add(domainEvent);

        // Update version
        _version = nextVersion;
    }

    /// <summary>
    /// Applies a historical event to rebuild aggregate state (no uncommitted events)
    /// </summary>
    /// <param name="domainEvent">The historical event to apply</param>
    internal void ApplyHistoricalEvent(DomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        ApplyEventInternal(domainEvent);
        _version = domainEvent.Version;
    }

    /// <summary>
    /// Applies an event to the aggregate state. Override in derived classes.
    /// </summary>
    /// <param name="domainEvent">The event to apply</param>
    protected abstract void ApplyEventInternal(DomainEvent domainEvent);

    /// <summary>
    /// Loads the aggregate from a series of historical events
    /// </summary>
    /// <param name="events">Historical events in order</param>
    public void LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        if (events == null)
            throw new ArgumentNullException(nameof(events));

        var eventList = events.OrderBy(e => e.Version).ToList();

        foreach (var domainEvent in eventList)
        {
            ApplyHistoricalEvent(domainEvent);
        }
    }

    /// <summary>
    /// Validates that the aggregate is in a consistent state
    /// </summary>
    protected virtual void ValidateInvariants()
    {
        // Override in derived classes to add validation rules
    }

    /// <summary>
    /// Ensures business invariants are maintained before applying events
    /// </summary>
    protected void EnsureInvariants()
    {
        try
        {
            ValidateInvariants();
        }
        catch (Exception ex)
        {
            throw new DomainException("Business rule validation failed", ex);
        }
    }
}

/// <summary>
/// Exception thrown when business rules are violated
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
