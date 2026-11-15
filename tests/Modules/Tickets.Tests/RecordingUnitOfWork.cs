using FabricPilot.Contracts;
using FabricPilot.Modules.Tickets.Application;

namespace FabricPilot.Modules.Tickets.Tests;

/// <summary>
/// The tickets unit of work over an <see cref="InMemoryTicketStore"/>: a commit saves the store and sends what was
/// published since the last one, as the outbox does; an event published without a commit is never sent.
/// </summary>
internal sealed class RecordingUnitOfWork(InMemoryTicketStore tickets) : ITicketsUnitOfWork
{
    private readonly List<IntegrationEvent> pending = [];

    public List<IntegrationEvent> Sent { get; } = [];

    public int Commits { get; private set; }

    public Task PublishAsync(IntegrationEvent integrationEvent)
    {
        pending.Add(integrationEvent);
        return Task.CompletedTask;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await tickets.SaveAsync(cancellationToken);
        Sent.AddRange(pending);
        pending.Clear();
        Commits++;
    }
}
