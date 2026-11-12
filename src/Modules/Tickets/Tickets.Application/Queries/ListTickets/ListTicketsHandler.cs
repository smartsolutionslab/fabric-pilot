using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

public sealed class ListTicketsHandler(ITicketStore tickets) : IQueryHandler<ListTickets, Result<PagedResult<TicketSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<TicketSummaryDto>, ApiError>> HandleAsync(ListTickets query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize, sorting) = query;
        Ensure.That(sorting).IsNotNull();
        Ensure.That((int)sorting.Field).Satisfies(field => Enum.IsDefined((TicketSortField)field), $"The ticket list cannot be sorted by {sorting.Field}.");
        Ensure.That((int)sorting.Direction).Satisfies(direction => Enum.IsDefined((SortDirection)direction), $"{sorting.Direction} is not a sort direction.");

        return Success(await tickets.ListAsync(sorting, page, pageSize, cancellationToken));
    }
}
