using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Infrastructure;
using FabricPilot.Modules.Billing.Infrastructure.Pdf;

namespace Microsoft.Extensions.DependencyInjection;

public static class BillingStorage
{
    /// <summary>The invoices table, read through the module's DbContext, and the PDF renderer, which keeps no state.</summary>
    public static IServiceCollection AddBillingStorage(this IServiceCollection services) =>
        services.AddScoped<IInvoiceStore, InvoiceStore>().AddSingleton<IInvoicePdfRenderer, PdfSharpInvoiceRenderer>();
}
