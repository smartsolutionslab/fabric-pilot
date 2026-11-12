namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record AttachmentTooLarge(string FileName, long MaximumSize)
    : DomainError("tickets.attachment-too-large", $"{FileName} is larger than {MaximumSize / (1024 * 1024)} MB, the largest file a ticket accepts.");
