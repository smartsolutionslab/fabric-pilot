namespace FabricPilot.Modules.Tickets.Domain.Tickets;

public sealed record InvalidAssignee(int Maximum)
    : DomainError("tickets.invalid-assignee", $"A ticket is assigned to a staff member's user name of 1 to {Maximum} characters.");
