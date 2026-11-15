using System.Linq.Expressions;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;

public static class TicketTriage
{
    /// <summary>The tickets the filter names; <paramref name="tenantOf"/> reads a ticket's tenant where the store keeps it.</summary>
    public static IQueryable<Ticket> Matching(this IQueryable<Ticket> tickets, TicketQueueFilter filter, Expression<Func<Ticket, string>> tenantOf)
    {
        Ensure.That(tickets).IsNotNull();
        Ensure.That(filter).IsNotNull();

        var (tenant, status, priority) = filter;
        if (tenant.HasValue)
        {
            var named = tenant.Value;
            tickets = tickets.Where(TicketTenantBinding.Bind<bool>((ticket, ticketTenant) => ticketTenant == named, tenantOf));
        }

        if (status.HasValue)
        {
            var named = status.Value;
            tickets = tickets.Where(ticket => ticket.Status == named);
        }

        if (priority.HasValue)
        {
            var named = priority.Value;
            tickets = tickets.Where(ticket => ticket.Priority == named);
        }

        return tickets;
    }
}
