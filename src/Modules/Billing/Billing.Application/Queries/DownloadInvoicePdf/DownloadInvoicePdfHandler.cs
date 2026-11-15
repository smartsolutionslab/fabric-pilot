using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Application.Queries.DownloadInvoicePdf;

/// <summary>
/// Reads the invoice through the tenant's rows only, so another tenant's invoice is not found; renders it on every
/// request from what is stored, so the PDF never differs from the invoice. The number is letters, digits and hyphens,
/// safe in a file name.
/// </summary>
public sealed class DownloadInvoicePdfHandler(IInvoiceStore invoices, IInvoicePdfRenderer renderer)
    : IQueryHandler<DownloadInvoicePdf, Result<InvoicePdf, ApiError>>
{
    public async Task<Result<InvoicePdf, ApiError>> HandleAsync(DownloadInvoicePdf query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var invoice = await invoices.ReadAsync(new InvoiceId(query.InvoiceId), cancellationToken);
        if (!invoice.HasValue)
        {
            return Failure(BillingFailures.InvoiceNotFound(query.InvoiceId));
        }

        return Success(new InvoicePdf($"invoice-{invoice.Value.Number}.pdf", renderer.Render(invoice.Value)));
    }
}
