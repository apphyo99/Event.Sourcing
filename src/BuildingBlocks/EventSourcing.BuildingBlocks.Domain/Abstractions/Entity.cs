namespace EventSourcing.BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Base class for all entities in the domain
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public abstract object GetId();

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity
    /// </summary>
    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetId().Equals(other.GetId());
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Equals(entity);
    }

    /// <summary>
    /// Returns the hash code for this entity
    /// </summary>
    public override int GetHashCode()
    {
        return GetId().GetHashCode();
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
