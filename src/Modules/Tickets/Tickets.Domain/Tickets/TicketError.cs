namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>The ways raising a ticket fails: a new one fails the build until the Application maps it.</summary>
public union TicketError(InvalidSubject, InvalidDescription, TooManyAttachments, AttachmentTooLarge, InvalidAttachmentName);
