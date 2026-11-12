namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record InvalidSubject(int Maximum)
    : DomainError("tickets.invalid-subject", $"A ticket needs a subject of 1 to {Maximum} characters.");
