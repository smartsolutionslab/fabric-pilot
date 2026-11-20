using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;

/// <summary>One page of every tenant's tickets for staff to triage, filtered and in the order asked for.</summary>
public sealed record ListTriageTickets(Page Page, PageSize PageSize, Sorting<TicketSortField> Sorting, TicketQueueFilter Filter)
    : IQuery<Result<PagedResult<QueuedTicketDto>, ApiError>>;
