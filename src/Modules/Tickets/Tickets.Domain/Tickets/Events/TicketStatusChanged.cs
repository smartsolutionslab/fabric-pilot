using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Tickets.Domain.Tickets.Events;

[DeliberatelyUnhandled("ChangeTicketStatusHandler publishes TicketStatusChangedV1 with the change, in the same transaction; nothing in the module follows it.")]
public sealed record TicketStatusChanged(TicketId Ticket, TicketStatus PreviousStatus, TicketStatus Status) : IDomainEvent;
