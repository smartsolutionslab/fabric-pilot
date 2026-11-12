namespace FabricPilot.Modules.Tickets.Application.DTOs;

public sealed record TicketDto(Guid Id, string Subject, string Description, string Priority, DateTimeOffset RaisedAt, IReadOnlyList<AttachmentDto> Attachments);
