namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record TooManyAttachments(int Maximum)
    : DomainError("tickets.too-many-attachments", $"A ticket takes at most {Maximum} attachments.");
