using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
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

    public Task<PagedResult<TicketSummaryDto>> ListAsync(Sorting<TicketSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var items = Saved.AsQueryable()
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(TicketMappings.ToTicketSummaryDtoExpression());
        return Task.FromResult(new PagedResult<TicketSummaryDto>([.. items], Saved.Count, page.Value, pageSize.Value));
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        Saved.AddRange(pending);
        pending.Clear();
        return Task.CompletedTask;
    }
}
