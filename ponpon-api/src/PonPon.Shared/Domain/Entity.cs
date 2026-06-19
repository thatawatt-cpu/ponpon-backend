namespace PonPon.Shared.Domain;

public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected Entity() => Id = Guid.NewGuid();

    public Guid Id { get; protected set; }
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
