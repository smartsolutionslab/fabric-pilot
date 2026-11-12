using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

/// <summary>One page of the caller's tenant's tickets, in the order asked for.</summary>
public sealed record ListTickets(Page Page, PageSize PageSize, Sorting<TicketSortField> Sorting)
    : IQuery<Result<PagedResult<TicketSummaryDto>, ApiError>>
{
    /// <summary>The order the list has when the caller asks for none.</summary>
    public static Sorting<TicketSortField> NewestFirst { get; } = new(TicketSortField.RaisedAt, SortDirection.Descending);
}
