namespace FabricPilot.Modules.Notifications.Application.DTOs;

/// <summary>A notice as its customer sees it.</summary>
public sealed record NoticeDto(Guid Id, Guid TicketId, string Text, DateTimeOffset CreatedAt);
