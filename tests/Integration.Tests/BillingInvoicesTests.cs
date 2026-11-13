using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FabricPilot.Modules.Billing.Domain.Invoices;
using UglyToad.PdfPig;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// A customer lists their own tenant's invoices and downloads each as a PDF whose text holds the number, the lines and
/// the total; another tenant's invoice is not found, never forbidden.
/// </summary>
public class BillingInvoicesTests(AspireFixture aspire)
{
    private const string Invoices = "/billing/invoices?page=1&pageSize=100";

    [Fact]
    public async Task A_customer_sees_their_tenants_invoices_and_not_another_tenants()
    {
        var own = Issued(Number(), ("Managed printing", 3, 49.90m), ("Toner", 2, 12.50m));
        var otherTenants = Issued(Number(), ("Hosting", 1, 300m));
        await InvoicesTable.RecordAsync(aspire, "tenant-a", [own], TestContext.Current.CancellationToken);
        await InvoicesTable.RecordAsync(aspire, "tenant-b", [otherTenants], TestContext.Current.CancellationToken);

        using var tenantA = await aspire.CreateHostClientAsync("developer");
        var items = (await Json(tenantA, Invoices))["items"]!.AsArray();

        var listed = items.Single(item => item!["id"]!.GetValue<Guid>() == own.Id.Value)!;
        listed["number"]!.GetValue<string>().ShouldBe(own.Number);
        listed["issuedOn"]!.GetValue<string>().ShouldBe("2026-11-01");
        listed["dueOn"]!.GetValue<string>().ShouldBe("2026-11-30");
        listed["currency"]!.GetValue<string>().ShouldBe("EUR");
        listed["total"]!.GetValue<decimal>().ShouldBe(174.70m);
        items.ShouldNotContain(item => item!["id"]!.GetValue<Guid>() == otherTenants.Id.Value);

        using var tenantB = await aspire.CreateHostClientAsync("developer-b");
        var otherItems = (await Json(tenantB, Invoices))["items"]!.AsArray();
        otherItems.ShouldContain(item => item!["id"]!.GetValue<Guid>() == otherTenants.Id.Value);
        otherItems.ShouldNotContain(item => item!["id"]!.GetValue<Guid>() == own.Id.Value);
    }

    [Fact]
    public async Task A_customer_downloads_an_invoice_as_a_pdf_holding_its_number_lines_and_total()
    {
        var invoice = Issued(Number(), ("Managed printing", 3, 49.90m), ("Toner cartridge", 2, 1234.50m));
        await InvoicesTable.RecordAsync(aspire, "tenant-a", [invoice], TestContext.Current.CancellationToken);
        using var client = await aspire.CreateHostClientAsync("developer");

        using var response = await client.GetAsync(Pdf(invoice.Id.Value), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/pdf");
        response.Content.Headers.ContentDisposition.ShouldNotBeNull().DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileNameStar.ShouldBe($"invoice-{invoice.Number}.pdf");
        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        var text = string.Join(' ', pdf.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));
        text.ShouldContain($"Invoice {invoice.Number}");
        text.ShouldContain("Managed printing 3 49.90 149.70");
        text.ShouldContain("Toner cartridge 2 1,234.50 2,469.00");
        text.ShouldContain("Total 2,618.70 EUR");
    }

    [Fact]
    public async Task An_invoice_issued_with_the_same_line_twice_is_stored_and_read_back_with_both_lines_and_their_total()
    {
        var toner = InvoiceLine.Describe("Toner", 2, 12.50m).Value;
        var invoice = Invoice.Issue(InvoiceId.New(), Number(), new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), "EUR", [toner, toner]).Value;
        await InvoicesTable.RecordAsync(aspire, "tenant-a", [invoice], TestContext.Current.CancellationToken);
        using var client = await aspire.CreateHostClientAsync("developer");

        var listed = (await Json(client, Invoices))["items"]!.AsArray().Single(item => item!["id"]!.GetValue<Guid>() == invoice.Id.Value)!;
        using var response = await client.GetAsync(Pdf(invoice.Id.Value), TestContext.Current.CancellationToken);

        listed["total"]!.GetValue<decimal>().ShouldBe(50.00m);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var pdf = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        var text = string.Join(' ', pdf.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));
        text.ShouldContain("Toner 2 12.50 25.00 Toner 2 12.50 25.00");
        text.ShouldContain("Total 50.00 EUR");
    }

    [Fact]
    public async Task Another_tenants_invoice_is_not_found_rather_than_forbidden()
    {
        var otherTenants = Issued(Number(), ("Hosting", 1, 300m));
        await InvoicesTable.RecordAsync(aspire, "tenant-b", [otherTenants], TestContext.Current.CancellationToken);
        using var tenantA = await aspire.CreateHostClientAsync("developer");

        using var response = await tenantA.GetAsync(Pdf(otherTenants.Id.Value), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        problem!["code"]!.GetValue<string>().ShouldBe("billing.invoice-not-found");
    }

    [Fact]
    public async Task An_invoice_that_does_not_exist_is_not_found()
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        using var response = await client.GetAsync(Pdf(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        problem!["code"]!.GetValue<string>().ShouldBe("billing.invoice-not-found");
    }

    [Theory]
    [InlineData("visitor")]
    [InlineData("staff")]
    public async Task A_user_without_the_customer_role_is_forbidden_the_list_and_the_pdf(string user)
    {
        var invoice = Issued(Number(), ("Toner", 1, 12.50m));
        await InvoicesTable.RecordAsync(aspire, "tenant-a", [invoice], TestContext.Current.CancellationToken);
        using var client = await aspire.CreateHostClientAsync(user);

        using var list = await client.GetAsync(new Uri(Invoices, UriKind.Relative), TestContext.Current.CancellationToken);
        using var pdf = await client.GetAsync(Pdf(invoice.Id.Value), TestContext.Current.CancellationToken);

        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        pdf.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized_for_the_list_and_the_pdf()
    {
        using var client = aspire.CreateHostClient();

        using var list = await client.GetAsync(new Uri(Invoices, UriKind.Relative), TestContext.Current.CancellationToken);
        using var pdf = await client.GetAsync(Pdf(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        list.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        pdf.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("sortBy=Currency")]
    [InlineData("sortBy=99")]
    [InlineData("sortDirection=Sideways")]
    public async Task A_sort_the_list_does_not_offer_is_refused(string sort)
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        using var response = await client.GetAsync(new Uri($"/billing/invoices?{sort}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static Uri Pdf(Guid invoice) => new($"/billing/invoices/{invoice}/pdf", UriKind.Relative);

    /// <summary>A number of its own for every invoice a test records, in a database the tests share: a Guid v7's random end.</summary>
    private static string Number() => "T-" + Guid.CreateVersion7().ToString("N")[2..];

    private static Invoice Issued(string number, params (string Description, int Quantity, decimal UnitPrice)[] lines)
    {
        List<InvoiceLine> described = [.. lines.Select(line => InvoiceLine.Describe(line.Description, line.Quantity, line.UnitPrice).Value)];
        return Invoice.Issue(InvoiceId.New(), number, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), "EUR", described).Value;
    }

    private static async Task<JsonNode> Json(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }
}
