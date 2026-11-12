using System.Net;
using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Application.Queries.DownloadInvoicePdf;

namespace FabricPilot.Modules.Billing.Tests.Queries;

public class DownloadInvoicePdfTests
{
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];

    [Fact]
    public async Task An_invoice_of_the_tenant_is_rendered_and_named_after_its_number()
    {
        var store = new InMemoryInvoiceStore();
        var invoice = IssuedInvoices.Issued("2026-0042", new DateOnly(2026, 11, 1), ("Toner", 2, 12.50m));
        store.Saved.Add(invoice);
        var renderer = new RecordingInvoicePdfRenderer(Pdf);

        var result = await new DownloadInvoicePdfHandler(store, renderer).HandleAsync(new DownloadInvoicePdf(invoice.Id.Value), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("invoice-2026-0042.pdf");
        result.Value.Content.ShouldBe(Pdf);
        renderer.Rendered.ShouldHaveSingleItem().ShouldBeSameAs(invoice);
    }

    [Fact]
    public async Task An_invoice_the_tenant_does_not_have_is_not_found_and_nothing_is_rendered()
    {
        var store = new InMemoryInvoiceStore();
        store.Saved.Add(IssuedInvoices.Issued("2026-0042", new DateOnly(2026, 11, 1), ("Toner", 2, 12.50m)));
        var renderer = new RecordingInvoicePdfRenderer(Pdf);
        var unknown = Guid.CreateVersion7();

        var result = await new DownloadInvoicePdfHandler(store, renderer).HandleAsync(new DownloadInvoicePdf(unknown), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var notFound = result.Error.ShouldBeOfType<InvoiceNotFound>();
        notFound.Code.ShouldBe("billing.invoice-not-found");
        notFound.Status.ShouldBe(HttpStatusCode.NotFound);
        notFound.Invoice.ShouldBe(unknown);
        renderer.Rendered.ShouldBeEmpty();
    }
}
