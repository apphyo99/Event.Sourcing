namespace EventSourcing.BuildingBlocks.Domain.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Identifier of the aggregate stream
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Type of the aggregate stream
    /// </summary>
    public string StreamType { get; }

    /// <summary>
    /// Version of the event within the stream
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Type of the event (class name by default)
    /// </summary>
    public virtual string EventType => GetType().Name;

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Correlation identifier for tracking related events
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Causation identifier (ID of the command/event that caused this event)
    /// </summary>
    public string? CausationId { get; }

    /// <summary>
    /// Information about who/what triggered this event
    /// </summary>
    public EventActor Actor { get; }

    /// <summary>
    /// Schema version for event evolution
    /// </summary>
    public virtual int SchemaVersion => 1;

    /// <summary>
    /// Initializes a new domain event
    /// </summary>
    /// <param name="streamId">Identifier of the aggregate stream</param>
    /// <param name="streamType">Type of the aggregate stream</param>
    /// <param name="correlationId">Correlation identifier</param>
    /// <param name="causationId">Causation identifier</param>
    /// <param name="actor">Event actor information</param>
    protected DomainEvent(
        string streamId,
        string streamType,
        string correlationId,
        string? causationId,
        EventActor actor)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));
        if (string.IsNullOrWhiteSpace(streamType))
            throw new ArgumentException("Stream type cannot be null or empty", nameof(streamType));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        EventId = Guid.NewGuid();
        StreamId = streamId;
        StreamType = streamType;
        OccurredAt = DateTime.UtcNow;
        CorrelationId = correlationId;
        CausationId = causationId;
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
    }

    /// <summary>
    /// Sets the version of this event within the stream
    /// </summary>
    /// <param name="version">The version number</param>
    internal void SetVersion(int version)
    {
        if (version <= 0)
            throw new ArgumentException("Version must be greater than zero", nameof(version));

        Version = version;
    }
}

/// <summary>
/// Information about who/what triggered an event
/// </summary>
public class EventActor
{
    /// <summary>
    /// User or system identifier
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Display name for the actor
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Type of actor (User, System, etc.)
    /// </summary>
    public string ActorType { get; }

    /// <summary>
    /// Additional context about the actor
    /// </summary>
    public Dictionary<string, object>? Context { get; }

    /// <summary>
    /// Initializes a new event actor
    /// </summary>
    public EventActor(string userId, string? displayName = null, string actorType = "User", Dictionary<string, object>? context = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        UserId = userId;
        DisplayName = displayName;
        ActorType = actorType;
        Context = context;
    }

    /// <summary>
    /// Creates a system actor
    /// </summary>
    public static EventActor System(string systemName, Dictionary<string, object>? context = null)
    {
        return new EventActor(systemName, systemName, "System", context);
    }
}
