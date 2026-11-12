using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Billing.Application.Queries.DownloadInvoicePdf;

public sealed record DownloadInvoicePdf(Guid InvoiceId) : IQuery<Result<InvoicePdf, ApiError>>;
