namespace FabricPilot.Modules.Tickets.Application.DTOs;

/// <summary>An attachment as a ticket lists it; its content is downloaded on its own.</summary>
public sealed record AttachmentDto(Guid Id, string FileName, string ContentType, long Size);
