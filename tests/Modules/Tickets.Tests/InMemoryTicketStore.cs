using System.Linq.Expressions;
using FabricPilot.Modules.Tickets.Application;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Tests;

/// <summary>
/// The tickets table, in memory; what is added counts as stored only once saved. Each ticket belongs to the tenant
/// <see cref="Tenants"/> names for it, <c>tenant-a</c> unless it names another; the queue reads every tenant's, the
/// other reads stand for one tenant's request.
/// </summary>
internal sealed class InMemoryTicketStore : ITicketStore
{
    public const string DefaultTenant = "tenant-a";

    private readonly List<Ticket> pending = [];

    public List<Ticket> Saved { get; } = [];

    public Dictionary<TicketId, string> Tenants { get; } = [];

    /// <summary>How many times the store was saved; a unit of work that commits saves it once.</summary>
    public int Saves { get; private set; }

    private Expression<Func<Ticket, string>> TenantOf => ticket => Tenants.GetValueOrDefault(ticket.Id, DefaultTenant);

    public void Add(Ticket ticket) => pending.Add(ticket);

    public Task<Option<Ticket>> ReadAsync(TicketId id, CancellationToken cancellationToken) => Task.FromResult(Find(id));

    public Task<Option<Ticket>> FindAsync(TicketId id, CancellationToken cancellationToken) => Task.FromResult(Find(id));

    public Task<Option<TenantTicket>> ReadWithTenantAsync(TicketId id, CancellationToken cancellationToken)
    {
        var ticket = Find(id);
        return Task.FromResult(ticket.HasValue
            ? Option<TenantTicket>.Some(new TenantTicket(TenantOf.Compile()(ticket.Value), ticket.Value))
            : Option<TenantTicket>.None);
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

    public Task<PagedResult<QueuedTicketDto>> ListQueueAsync(
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

        var matching = Saved.AsQueryable().Matching(filter, TenantOf);
        var items = matching
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(TicketMappings.ToQueuedTicketDtoExpression(TenantOf));
        return Task.FromResult(new PagedResult<QueuedTicketDto>([.. items], matching.Count(), page.Value, pageSize.Value));
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        Saved.AddRange(pending);
        pending.Clear();
        Saves++;
        return Task.CompletedTask;
    }

    private Option<Ticket> Find(TicketId id)
    {
        var ticket = Saved.SingleOrDefault(candidate => candidate.Id == id);
        return ticket is null ? Option<Ticket>.None : Option<Ticket>.Some(ticket);
    }
}
