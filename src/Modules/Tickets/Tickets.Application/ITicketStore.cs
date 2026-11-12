using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

/// <summary>The tickets table, the current tenant's rows only; implemented in the module's Infrastructure project.</summary>
public interface ITicketStore
{
    void Add(Ticket ticket);

    /// <summary>The ticket with its attachments as it is stored, for a query: nothing that changes it is saved.</summary>
    Task<Option<Ticket>> ReadAsync(TicketId id, CancellationToken cancellationToken);

    /// <summary>One page of tickets, newest first, for a query.</summary>
    Task<PagedResult<Ticket>> ListAsync(Page page, PageSize pageSize, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
