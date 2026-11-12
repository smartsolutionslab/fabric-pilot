using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.GetTicket;

public sealed record GetTicket(Guid TicketId) : IQuery<Result<TicketDto, ApiError>>;
