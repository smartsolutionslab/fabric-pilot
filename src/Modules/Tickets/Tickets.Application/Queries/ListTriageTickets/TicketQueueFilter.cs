using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;

/// <summary>Which tickets the queue shows: those of the tenant, with the status and of the priority named; none named, every one.</summary>
public sealed record TicketQueueFilter(Option<string> Tenant, Option<TicketStatus> Status, Option<TicketPriority> Priority)
{
    public static TicketQueueFilter Everything { get; } = new(Option<string>.None, Option<TicketStatus>.None, Option<TicketPriority>.None);
}
