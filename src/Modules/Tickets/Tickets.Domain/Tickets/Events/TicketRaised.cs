using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Tickets.Domain.Tickets.Events;

[DeliberatelyUnhandled("Nothing follows a raised ticket yet; status-change notifications (#8) and the staff triage queue (#7) come later.")]
public sealed record TicketRaised(TicketId Ticket) : IDomainEvent;
