namespace Bulkivore.Api.Domain.Common;

public class AggregateRoot<TId> : Entity<TId>, IAggregateRoot where TId : struct
{
    protected AggregateRoot(TId id) : base(id)
    {
        _domainEvents = [];
    }

    private readonly List<IDomainEvent> _domainEvents;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
