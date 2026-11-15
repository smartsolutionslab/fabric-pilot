using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Application.DTOs;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.GetStaffTicket;

public sealed class GetStaffTicketHandler(ITicketStore tickets) : IQueryHandler<GetStaffTicket, Result<StaffTicketDto, ApiError>>
{
    public async Task<Result<StaffTicketDto, ApiError>> HandleAsync(GetStaffTicket query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var found = await tickets.ReadWithTenantAsync(new TicketId(query.TicketId), cancellationToken);
        if (!found.HasValue)
        {
            return Failure(TicketsFailures.TicketNotFound(query.TicketId));
        }

        return Success(found.Value.ToStaffTicketDto());
    }
}
