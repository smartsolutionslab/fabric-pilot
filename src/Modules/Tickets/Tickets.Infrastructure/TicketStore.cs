using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using FabricPilot.Modules.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Tickets.Infrastructure;

/// <summary>Every read is filtered to the current tenant by the DbContext's query filter.</summary>
public sealed class TicketStore(TicketsDbContext context) : ITicketStore
{
    public void Add(Ticket ticket) => context.Add(ticket);

    public async Task<Option<Ticket>> ReadAsync(TicketId id, CancellationToken cancellationToken)
    {
        var ticket = await context.Set<Ticket>().AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return ticket is null ? Option<Ticket>.None : Option<Ticket>.Some(ticket);
    }

    public async Task<PagedResult<Ticket>> ListAsync(Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var tickets = context.Set<Ticket>().AsNoTracking();
        var total = await tickets.CountAsync(cancellationToken);
        var items = await tickets
            .OrderByDescending(ticket => ticket.RaisedAt)
            .ThenByDescending(ticket => ticket.Id)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .ToListAsync(cancellationToken);
        return new PagedResult<Ticket>(items, total, page.Value, pageSize.Value);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
