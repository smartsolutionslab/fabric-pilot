using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
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

    public async Task<PagedResult<TicketSummaryDto>> ListAsync(Sorting<TicketSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var tickets = context.Set<Ticket>().AsNoTracking();
        var total = await tickets.CountAsync(cancellationToken);
        var items = await tickets
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(TicketMappings.ToTicketSummaryDtoExpression())
            .ToListAsync(cancellationToken);
        return new PagedResult<TicketSummaryDto>(items, total, page.Value, pageSize.Value);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
