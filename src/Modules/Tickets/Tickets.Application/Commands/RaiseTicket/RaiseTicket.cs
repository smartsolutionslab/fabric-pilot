using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;

/// <summary>Raises a ticket in the caller's tenant, recording the caller as its customer; answers the new ticket's id.</summary>
public sealed record RaiseTicket(string Subject, string Description, string Priority, TicketCustomer Customer, IReadOnlyList<NewAttachment> Attachments)
    : ICommand<Result<Guid, ApiError>>;
