namespace EventSourcing.BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Base class for value objects in the domain
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components used for equality comparison
    /// </summary>
    /// <returns>Collection of components that define equality</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified value object is equal to the current value object
    /// </summary>
    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current value object
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is ValueObject valueObject && Equals(valueObject);
    }

    /// <summary>
    /// Returns the hash code for this value object
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Where(component => component != null)
            .Aggregate(1, (current, component) => HashCode.Combine(current, component!.GetHashCode()));
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
