using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Commands.AssignTicket;

/// <summary>
/// Saves the assignment together with <see cref="TicketAssignedV1"/>. A ticket assigned to that staff member already
/// changes nothing and announces nothing.
/// </summary>
public sealed class AssignTicketHandler(ITicketStore tickets, ITicketsUnitOfWork unitOfWork)
    : ICommandHandler<AssignTicket, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(AssignTicket command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (ticketId, assignee) = command;
        var found = await tickets.FindAsync(new TicketId(ticketId), cancellationToken);
        if (!found.HasValue)
        {
            return Failure(TicketsFailures.TicketNotFound(ticketId));
        }

        var ticket = found.Value;
        var assigned = ticket.AssignTo(assignee);
        if (assigned.IsFailure)
        {
            return Failure(TicketsFailures.From(assigned.Error));
        }

        if (!assigned.Value)
        {
            return Success(Unit.Value);
        }

        await unitOfWork.PublishAsync(new TicketAssignedV1(ticket.Id.Value, ticket.Subject, ticket.Assignee!));
        await unitOfWork.CommitAsync(cancellationToken);
        return Success(Unit.Value);
    }
}
