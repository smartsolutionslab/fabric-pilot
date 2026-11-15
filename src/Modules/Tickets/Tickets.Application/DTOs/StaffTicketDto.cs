namespace FabricPilot.Modules.Tickets.Application.DTOs;

/// <summary>A ticket as staff read it across tenants: the tenant it belongs to, which a change to it names, beside the ticket.</summary>
public sealed record StaffTicketDto(
    Guid Id,
    string Tenant,
    string Subject,
    string Description,
    string Priority,
    string Status,
    string? Assignee,
    DateTimeOffset RaisedAt,
    IReadOnlyList<AttachmentDto> Attachments);
