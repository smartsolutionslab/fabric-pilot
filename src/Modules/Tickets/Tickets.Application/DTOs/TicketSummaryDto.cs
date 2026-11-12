namespace FabricPilot.Modules.Tickets.Application.DTOs;

/// <summary>A ticket as the list shows it.</summary>
public sealed record TicketSummaryDto(Guid Id, string Subject, string Priority, DateTimeOffset RaisedAt, int AttachmentCount);
