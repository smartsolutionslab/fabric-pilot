using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application.DTOs;

public static class AttachmentMappings
{
    public static AttachmentDto ToDto(this Attachment attachment)
    {
        Ensure.That(attachment).IsNotNull();
        return new AttachmentDto(attachment.Id.Value, attachment.FileName, attachment.ContentType, attachment.Size);
    }
}
