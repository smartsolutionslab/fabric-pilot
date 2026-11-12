using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Tickets.Application.Commands.RaiseTicket;

/// <summary>Raises a ticket in the caller's tenant; answers the new ticket's id.</summary>
public sealed record RaiseTicket(string Subject, string Description, string Priority, IReadOnlyList<NewAttachment> Attachments)
    : ICommand<Result<Guid, ApiError>>;
