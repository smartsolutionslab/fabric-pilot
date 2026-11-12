using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.GetTicket;

public sealed class GetTicketHandler(ITicketStore tickets) : IQueryHandler<GetTicket, Result<TicketDto, ApiError>>
{
    public async Task<Result<TicketDto, ApiError>> HandleAsync(GetTicket query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var found = await tickets.ReadAsync(new TicketId(query.TicketId), cancellationToken);
        if (!found.HasValue)
        {
            return Failure(TicketsFailures.TicketNotFound(query.TicketId));
        }

        return Success(found.Value.ToTicketDto());
    }
}
