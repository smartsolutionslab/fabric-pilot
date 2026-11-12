namespace FabricPilot.BuildingBlocks.Domain;

/// <summary>The entry point to a consistency boundary. Records the domain events its changes raise until they are dispatched.</summary>
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id), IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => domainEvents;

    public void ClearDomainEvents() => domainEvents.Clear();

    public void RemoveDomainEvents(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        Ensure.That(domainEvents).IsNotNull();
        this.domainEvents.RemoveAll(domainEvent => domainEvents.Contains(domainEvent, ReferenceEqualityComparer.Instance));
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        Ensure.That(domainEvent).IsNotNull();
        domainEvents.Add(domainEvent);
    }
}
