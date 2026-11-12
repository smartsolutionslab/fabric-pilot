namespace FabricPilot.Modules.Tickets.Application.Queries.DownloadAttachment;

/// <summary>An attachment's stored content and the name it was attached with; the caller disposes the content.</summary>
public sealed record AttachmentDownload(string FileName, Stream Content);
