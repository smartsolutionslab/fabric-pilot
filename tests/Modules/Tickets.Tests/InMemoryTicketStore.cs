using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests;

/// <summary>The tickets table of one tenant, in memory; what is added counts as stored only once saved.</summary>
internal sealed class InMemoryTicketStore : ITicketStore
{
    private readonly List<Ticket> pending = [];

    public List<Ticket> Saved { get; } = [];

    public void Add(Ticket ticket) => pending.Add(ticket);

    public Task<Option<Ticket>> ReadAsync(TicketId id, CancellationToken cancellationToken)
    {
        var ticket = Saved.SingleOrDefault(candidate => candidate.Id == id);
        return Task.FromResult(ticket is null ? Option<Ticket>.None : Option<Ticket>.Some(ticket));
    }

    public Task<PagedResult<Ticket>> ListAsync(Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var newestFirst = Saved.OrderByDescending(ticket => ticket.RaisedAt).ThenByDescending(ticket => ticket.Id.Value).ToList();
        return Task.FromResult(new PagedResult<Ticket>([.. newestFirst.Skip(page.Skip(pageSize)).Take(pageSize.Value)], newestFirst.Count, page.Value, pageSize.Value));
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        Saved.AddRange(pending);
        pending.Clear();
        return Task.CompletedTask;
    }
}
