using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

/// <summary>Where an attachment's content is kept in the <see cref="IBlobStore"/>.</summary>
public static class AttachmentBlobs
{
    public static string Name(TicketId ticket, AttachmentId attachment)
    {
        Ensure.That(ticket).IsNotNull();
        Ensure.That(attachment).IsNotNull();

        return $"{ticket.Value}/{attachment.Value}";
    }
}
