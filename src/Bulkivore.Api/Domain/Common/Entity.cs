namespace Bulkivore.Api.Domain.Common;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; }

    protected Entity(TId id) => Id = id;


    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Equals(other);
    }

    private bool Equals(Entity<TId>? other) => Id.Equals(other?.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
