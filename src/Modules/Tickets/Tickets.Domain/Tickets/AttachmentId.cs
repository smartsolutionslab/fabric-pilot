namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record AttachmentId(Guid Value)
{
    public static AttachmentId New() => new(Guid.CreateVersion7());
}
