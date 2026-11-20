using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Tickets.Application.Commands.AssignTicket;

/// <summary>Assigns a ticket of the current tenant to the staff member with the user name; staff send it for the tenant their route names.</summary>
public sealed record AssignTicket(Guid TicketId, string Assignee) : ICommand<Result<Unit, ApiError>>;
