using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Tests;

/// <summary>Answers every invoice with the same bytes and keeps the invoices it was asked to render.</summary>
internal sealed class RecordingInvoicePdfRenderer(byte[] content) : IInvoicePdfRenderer
{
    public List<Invoice> Rendered { get; } = [];

    public byte[] Render(Invoice invoice)
    {
        Rendered.Add(invoice);
        return content;
    }
}
