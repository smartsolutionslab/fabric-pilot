namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>Every rule a ticket can break, each with its own code.</summary>
public static class TicketFailures
{
    public static DomainError InvalidSubject(int maximum) => new InvalidSubject(maximum);

    public static DomainError InvalidDescription(int maximum) => new InvalidDescription(maximum);

    public static DomainError TooManyAttachments(int maximum) => new TooManyAttachments(maximum);

    public static DomainError AttachmentTooLarge(string fileName, long maximumSize) => new AttachmentTooLarge(fileName, maximumSize);

    public static DomainError InvalidAttachmentName(int maximum) => new InvalidAttachmentName(maximum);
}
