namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record InvalidAttachmentName(int Maximum)
    : DomainError("tickets.invalid-attachment-name", $"An attachment needs a file name of 1 to {Maximum} characters.");
