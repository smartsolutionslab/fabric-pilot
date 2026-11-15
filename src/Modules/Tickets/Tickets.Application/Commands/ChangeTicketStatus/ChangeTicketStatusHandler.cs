using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Contracts.Tickets;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Commands.ChangeTicketStatus;

/// <summary>
/// Saves the new status together with <see cref="TicketStatusChangedV1"/>, which tells the customer who raised the
/// ticket. A ticket that has the status already changes nothing and tells nobody, so a request sent twice notifies once.
/// </summary>
public sealed class ChangeTicketStatusHandler(ITicketStore tickets, ITicketsUnitOfWork unitOfWork)
    : ICommandHandler<ChangeTicketStatus, Result<Unit, ApiError>>
{
    public async Task<Result<Unit, ApiError>> HandleAsync(ChangeTicketStatus command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (ticketId, statusName) = command;
        if (!Enum.GetNames<TicketStatus>().Contains(statusName, StringComparer.Ordinal))
        {
            return Failure(TicketsFailures.UnknownStatus(statusName));
        }

        var found = await tickets.FindAsync(new TicketId(ticketId), cancellationToken);
        if (!found.HasValue)
        {
            return Failure(TicketsFailures.TicketNotFound(ticketId));
        }

        var ticket = found.Value;
        var previous = ticket.Status;
        if (!ticket.ChangeStatus(Enum.Parse<TicketStatus>(statusName)))
        {
            return Success(Unit.Value);
        }

        var statusChanged = new TicketStatusChangedV1(
            ticket.Id.Value,
            ticket.Subject,
            previous.ToString(),
            ticket.Status.ToString(),
            ticket.Customer.UserId,
            ticket.Customer.Email);
        await unitOfWork.PublishAsync(statusChanged);
        await unitOfWork.CommitAsync(cancellationToken);
        return Success(Unit.Value);
    }
}
