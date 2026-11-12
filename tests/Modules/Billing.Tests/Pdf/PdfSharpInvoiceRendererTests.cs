using FabricPilot.Modules.Billing.Domain.Invoices;
using FabricPilot.Modules.Billing.Infrastructure.Pdf;
using UglyToad.PdfPig;

namespace FabricPilot.Modules.Billing.Tests.Pdf;

/// <summary>Renders invoices with PDFsharp and reads the text back out of the PDF with PdfPig.</summary>
public class PdfSharpInvoiceRendererTests
{
    [Fact]
    public void The_pdf_holds_the_invoice_number_its_dates_every_line_and_the_total()
    {
        var invoice = Invoice.Issue(
            InvoiceId.New(),
            "2026-0042",
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            "EUR",
            [Line("Managed printing", 3, 49.90m), Line("Toner cartridge", 2, 1234.50m)]).Value;

        var text = Text(new PdfSharpInvoiceRenderer().Render(invoice));

        text.ShouldContain("Invoice 2026-0042");
        text.ShouldContain("2026-11-01");
        text.ShouldContain("2026-11-30");
        text.ShouldContain("Managed printing 3 49.90 149.70");
        text.ShouldContain("Toner cartridge 2 1,234.50 2,469.00");
        text.ShouldContain("Total 2,618.70 EUR");
    }

    [Fact]
    public void An_invoice_with_the_most_lines_of_the_longest_descriptions_is_rendered_whole_over_several_pages()
    {
        List<InvoiceLine> lines = [.. Enumerable.Range(1, Invoice.MaximumLines)
            .Select(number => Line($"Line {number:000} " + string.Join(' ', Enumerable.Repeat("description", 20))[..(InvoiceLine.MaximumDescriptionLength - 9)], 1, 1m))];
        var invoice = Invoice.Issue(InvoiceId.New(), "2026-0043", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), "EUR", lines).Value;

        var pdf = new PdfSharpInvoiceRenderer().Render(invoice);

        using var document = PdfDocument.Open(pdf);
        document.NumberOfPages.ShouldBeGreaterThan(1);
        var text = Text(pdf);
        foreach (var number in Enumerable.Range(1, Invoice.MaximumLines))
        {
            text.ShouldContain($"Line {number:000} description");
        }

        text.ShouldContain("Total 100.00 EUR");
    }

    private static InvoiceLine Line(string description, int quantity, decimal unitPrice) =>
        InvoiceLine.Describe(description, quantity, unitPrice).Value;

    /// <summary>Every word of every page, in reading order, one space apart.</summary>
    private static string Text(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join(' ', document.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));
    }
}
