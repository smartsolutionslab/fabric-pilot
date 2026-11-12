namespace FabricPilot.Modules.Tickets.Domain.Tickets;

/// <summary>A file a ticket is raised with, as the customer sent it: its name may still carry a path.</summary>
public sealed record AttachmentFile(AttachmentId Id, string FileName, string ContentType, long Size);
