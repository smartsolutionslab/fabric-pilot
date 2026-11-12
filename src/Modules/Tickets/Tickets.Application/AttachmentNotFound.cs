using System.Net;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record AttachmentNotFound(Guid Ticket, Guid Attachment)
    : ApiError("tickets.attachment-not-found", $"Ticket {Ticket} has no attachment {Attachment}.", HttpStatusCode.NotFound);
