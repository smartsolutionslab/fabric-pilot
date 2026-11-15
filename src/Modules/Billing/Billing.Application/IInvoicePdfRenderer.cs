using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Application;

/// <summary>Lays an invoice out as a PDF document; implemented in the module's Infrastructure project, the one place that names the PDF library.</summary>
public interface IInvoicePdfRenderer
{
    byte[] Render(Invoice invoice);
}
