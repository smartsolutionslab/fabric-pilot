namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>Every rule a ticket can break, each with its own code.</summary>
public static class TicketErrors
{
    public static TicketError InvalidSubject(int maximum) => new InvalidSubject(maximum);

    public static TicketError InvalidDescription(int maximum) => new InvalidDescription(maximum);

    public static TicketError TooManyAttachments(int maximum) => new TooManyAttachments(maximum);

    public static TicketError AttachmentTooLarge(string fileName, long maximumSize) => new AttachmentTooLarge(fileName, maximumSize);

    public static TicketError InvalidAttachmentName(int maximum) => new InvalidAttachmentName(maximum);
}
