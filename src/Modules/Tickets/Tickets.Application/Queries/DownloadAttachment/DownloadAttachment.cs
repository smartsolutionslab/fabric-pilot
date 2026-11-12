using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Tickets.Application.Queries.DownloadAttachment;

public sealed record DownloadAttachment(Guid TicketId, Guid AttachmentId) : IQuery<Result<AttachmentDownload, ApiError>>;
