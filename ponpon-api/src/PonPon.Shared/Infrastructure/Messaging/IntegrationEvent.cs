namespace PonPon.Shared.Infrastructure.Messaging;

public abstract record IntegrationEvent(Guid Id, DateTime OccurredOnUtc)
{
    protected IntegrationEvent() : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }
}
