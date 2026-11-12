using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.Queries.DownloadAttachment;

/// <summary>
/// Reads the ticket first, through the tenant's rows only, so a file is found only by the tenant whose ticket names
/// it; the blob's name alone opens nothing.
/// </summary>
public sealed class DownloadAttachmentHandler(ITicketStore tickets, IBlobStore blobs)
    : IQueryHandler<DownloadAttachment, Result<AttachmentDownload, ApiError>>
{
    public async Task<Result<AttachmentDownload, ApiError>> HandleAsync(DownloadAttachment query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (ticketId, attachmentId) = query;
        var ticket = await tickets.ReadAsync(new TicketId(ticketId), cancellationToken);
        if (!ticket.HasValue)
        {
            return Failure(TicketsFailures.TicketNotFound(ticketId));
        }

        var attachment = ticket.Value.Attachments.SingleOrDefault(candidate => candidate.Id.Value == attachmentId);
        if (attachment is null)
        {
            return Failure(TicketsFailures.AttachmentNotFound(ticketId, attachmentId));
        }

        var content = await blobs.OpenReadAsync(AttachmentBlobs.Name(ticket.Value.Id, attachment.Id), cancellationToken);
        if (!content.HasValue)
        {
            return Failure(TicketsFailures.AttachmentNotFound(ticketId, attachmentId));
        }

        return Success(new AttachmentDownload(attachment.FileName, content.Value));
    }
}
