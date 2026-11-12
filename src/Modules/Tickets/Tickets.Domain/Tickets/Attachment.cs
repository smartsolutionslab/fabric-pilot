using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>A file attached to a ticket; its content lives in blob storage, not on the ticket.</summary>
public sealed class Attachment : Entity<AttachmentId>
{
    /// <summary>The largest file a ticket accepts, 10 MB.</summary>
    public const long MaximumSize = 10 * 1024 * 1024;

    public const int MaximumFileNameLength = 255;

    public const int MaximumContentTypeLength = 255;

    private const string UnknownContentType = "application/octet-stream";

    private Attachment(AttachmentId id, int position, string fileName, string contentType, long size)
        : base(id)
    {
        Position = position;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
    }

    /// <summary>Where the customer put the file among the ticket's attachments, from 1.</summary>
    public int Position { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long Size { get; private set; }

    internal static Result<Attachment, DomainError> From(AttachmentFile file, int position)
    {
        // A browser may send the path the file came from (C:\fakepath\report.pdf); only its name is kept.
        var fileName = file.FileName[(file.FileName.LastIndexOfAny(['/', '\\']) + 1)..].Trim();
        if (fileName.Length == 0 || fileName.Length > MaximumFileNameLength)
        {
            return Failure(TicketFailures.InvalidAttachmentName(MaximumFileNameLength));
        }

        if (file.Size > MaximumSize)
        {
            return Failure(TicketFailures.AttachmentTooLarge(fileName, MaximumSize));
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) || file.ContentType.Length > MaximumContentTypeLength
            ? UnknownContentType
            : file.ContentType.Trim();
        return Success(new Attachment(file.Id, position, fileName, contentType, file.Size));
    }
}
