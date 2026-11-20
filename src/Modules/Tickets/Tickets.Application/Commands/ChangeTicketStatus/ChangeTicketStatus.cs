using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Tickets.Application.Commands.ChangeTicketStatus;

/// <summary>Moves a ticket of the current tenant to the status named; staff send it for the tenant their route names.</summary>
public sealed record ChangeTicketStatus(Guid TicketId, string Status) : ICommand<Result<Unit, ApiError>>;
