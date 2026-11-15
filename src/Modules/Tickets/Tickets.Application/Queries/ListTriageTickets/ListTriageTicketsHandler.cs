using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Application.Queries.ListTickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.ListTriageTickets;

public sealed class ListTriageTicketsHandler(ITicketStore tickets) : IQueryHandler<ListTriageTickets, Result<PagedResult<QueuedTicketDto>, ApiError>>
{
    public async Task<Result<PagedResult<QueuedTicketDto>, ApiError>> HandleAsync(ListTriageTickets query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize, sorting, filter) = query;
        Ensure.That(sorting).IsNotNull();
        Ensure.That(filter).IsNotNull();
        Ensure.That((int)sorting.Field, nameof(sorting)).Satisfies(field => Enum.IsDefined((TicketSortField)field), $"The ticket queue cannot be sorted by {sorting.Field}.");
        Ensure.That((int)sorting.Direction, nameof(sorting)).Satisfies(direction => Enum.IsDefined((SortDirection)direction), $"{sorting.Direction} is not a sort direction.");
        if (filter.Status.HasValue)
        {
            Ensure.That((int)filter.Status.Value, nameof(filter)).Satisfies(status => Enum.IsDefined((TicketStatus)status), $"{filter.Status.Value} is not a status.");
        }

        if (filter.Priority.HasValue)
        {
            Ensure.That((int)filter.Priority.Value, nameof(filter)).Satisfies(priority => Enum.IsDefined((TicketPriority)priority), $"{filter.Priority.Value} is not a priority.");
        }

        return Success(await tickets.ListQueueAsync(filter, sorting, page, pageSize, cancellationToken));
    }
}
