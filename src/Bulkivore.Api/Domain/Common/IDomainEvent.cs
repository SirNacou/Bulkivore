namespace Bulkivore.Api.Domain.Common;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
}
