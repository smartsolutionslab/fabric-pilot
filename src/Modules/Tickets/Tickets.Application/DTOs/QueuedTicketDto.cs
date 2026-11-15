namespace FabricPilot.Modules.Tickets.Application.DTOs;

/// <summary>A ticket as the staff triage queue shows it: whose tenant it is, where it stands and whom it is assigned to.</summary>
public sealed record QueuedTicketDto(
    Guid Id,
    string Tenant,
    string Subject,
    string Priority,
    string Status,
    string? Assignee,
    DateTimeOffset RaisedAt,
    int AttachmentCount);
