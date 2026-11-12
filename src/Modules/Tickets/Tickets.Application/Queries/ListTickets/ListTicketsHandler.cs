using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTickets;

public sealed class ListTicketsHandler(ITicketStore tickets) : IQueryHandler<ListTickets, Result<PagedResult<TicketSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<TicketSummaryDto>, ApiError>> HandleAsync(ListTickets query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize) = query;
        var found = await tickets.ListAsync(page, pageSize, cancellationToken);
        return Success(new PagedResult<TicketSummaryDto>([.. found.Items.Select(ticket => ticket.ToTicketSummaryDto())], found.TotalCount, found.Page, found.PageSize));
    }
}
