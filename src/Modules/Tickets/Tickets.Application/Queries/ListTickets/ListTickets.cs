using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

/// <summary>One page of the caller's tenant's tickets, newest first.</summary>
public sealed record ListTickets(Page Page, PageSize PageSize) : IQuery<Result<PagedResult<TicketSummaryDto>, ApiError>>;
