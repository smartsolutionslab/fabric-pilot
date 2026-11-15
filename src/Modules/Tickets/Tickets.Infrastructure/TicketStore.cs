using FabricPilot.Modules.Tickets.Application;
using System.Linq.Expressions;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;
using FabricPilot.Modules.Tickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Tickets.Infrastructure;

/// <summary>
/// Every read is filtered to the current tenant by the DbContext's query filter, or reads every tenant's rows inside
/// <c>AcrossTenants()</c>.
/// </summary>
public sealed class TicketStore(TicketsDbContext context) : ITicketStore
{
    /// <summary>The tenant column's name, which the tenancy building block gives every tenant's table as a shadow property.</summary>
    private const string TenantProperty = "TenantId";

    /// <summary>Reads a ticket's tenant in a query: its shadow property, never a property of the domain class.</summary>
    public static Expression<Func<Ticket, string>> TenantOf { get; } = ticket => EF.Property<string>(ticket, TenantProperty);

    public void Add(Ticket ticket) => context.Add(ticket);

    public async Task<Option<Ticket>> ReadAsync(TicketId id, CancellationToken cancellationToken)
    {
        var ticket = await context.Set<Ticket>().AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return ticket is null ? Option<Ticket>.None : Option<Ticket>.Some(ticket);
    }

    public async Task<Option<Ticket>> FindAsync(TicketId id, CancellationToken cancellationToken)
    {
        var ticket = await context.Set<Ticket>().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return ticket is null ? Option<Ticket>.None : Option<Ticket>.Some(ticket);
    }

    public async Task<Option<TenantTicket>> ReadWithTenantAsync(TicketId id, CancellationToken cancellationToken)
    {
        var found = await context.Set<Ticket>()
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(ticket => new { Tenant = EF.Property<string>(ticket, TenantProperty), Ticket = ticket })
            .SingleOrDefaultAsync(cancellationToken);
        return found is null ? Option<TenantTicket>.None : Option<TenantTicket>.Some(new TenantTicket(found.Tenant, found.Ticket));
    }

    public async Task<PagedResult<QueuedTicketDto>> ListQueueAsync(
        TicketQueueFilter filter,
        Sorting<TicketSortField> sorting,
        Page page,
        PageSize pageSize,
        CancellationToken cancellationToken)
    {
        Ensure.That(filter).IsNotNull();
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var tickets = context.Set<Ticket>().AsNoTracking().Matching(filter, TenantOf);
        var total = await tickets.CountAsync(cancellationToken);
        var items = await tickets
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(TicketMappings.ToQueuedTicketDtoExpression(TenantOf))
            .ToListAsync(cancellationToken);
        return new PagedResult<QueuedTicketDto>(items, total, page.Value, pageSize.Value);
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
