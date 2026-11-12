using System.Net;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

/// <summary>Everything a tickets use case refuses, each with its own code.</summary>
public static class TicketsFailures
{
    /// <summary>A ticket rule the request broke: the domain's code, answered as 422.</summary>
    public static ApiError From(TicketError error) => error switch
    {
        InvalidSubject invalidSubject => ApiError.From(invalidSubject, HttpStatusCode.UnprocessableEntity),
        InvalidDescription invalidDescription => ApiError.From(invalidDescription, HttpStatusCode.UnprocessableEntity),
        TooManyAttachments tooManyAttachments => ApiError.From(tooManyAttachments, HttpStatusCode.UnprocessableEntity),
        AttachmentTooLarge attachmentTooLarge => ApiError.From(attachmentTooLarge, HttpStatusCode.UnprocessableEntity),
        InvalidAttachmentName invalidAttachmentName => ApiError.From(invalidAttachmentName, HttpStatusCode.UnprocessableEntity),
    };

    public static ApiError UnknownPriority(string priority) => new UnknownPriority(priority);

    public static ApiError TicketNotFound(Guid ticket) => new TicketNotFound(ticket);

    public static ApiError AttachmentNotFound(Guid ticket, Guid attachment) => new AttachmentNotFound(ticket, attachment);
}
