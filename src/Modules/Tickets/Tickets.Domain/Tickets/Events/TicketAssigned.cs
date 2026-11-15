using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Tickets.Domain.Tickets.Events;

[DeliberatelyUnhandled("AssignTicketHandler publishes TicketAssignedV1 with the assignment, in the same transaction; nothing in the module follows it.")]
public sealed record TicketAssigned(TicketId Ticket, string Assignee) : IDomainEvent;
