namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record InvalidDescription(int Maximum)
    : DomainError("tickets.invalid-description", $"A ticket needs a description of 1 to {Maximum} characters.");
