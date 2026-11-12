using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Tests.Invoices;

public class InvoiceTests
{
    private static readonly DateOnly IssuedOn = new(2026, 11, 1);

    private static readonly DateOnly DueOn = new(2026, 11, 30);

    [Fact]
    public void An_invoice_is_issued_with_its_number_dates_currency_and_lines_in_order()
    {
        var id = InvoiceId.New();

        var result = Invoice.Issue(id, "  2026-0042  ", IssuedOn, DueOn, "EUR", [Line("Managed printing", 3, 49.90m), Line("Toner", 2, 12.50m)]);

        result.IsSuccess.ShouldBeTrue();
        var invoice = result.Value;
        invoice.Id.ShouldBe(id);
        invoice.Number.ShouldBe("2026-0042");
        invoice.IssuedOn.ShouldBe(IssuedOn);
        invoice.DueOn.ShouldBe(DueOn);
        invoice.Currency.ShouldBe("EUR");
        invoice.Lines.Select(line => line.Description).ShouldBe(["Managed printing", "Toner"]);
        invoice.Lines.Select(line => line.Position).ShouldBe([1, 2]);
    }

    [Fact]
    public void A_lines_amount_is_its_quantity_times_its_unit_price_and_the_total_is_the_sum_of_the_amounts()
    {
        var invoice = Invoice.Issue(InvoiceId.New(), "2026-0042", IssuedOn, DueOn, "EUR", [Line("Managed printing", 3, 49.90m), Line("Toner", 2, 12.50m)]).Value;

        invoice.Lines.Select(line => line.Amount).ShouldBe([149.70m, 25.00m]);
        invoice.Total.ShouldBe(174.70m);
    }

    [Fact]
    public void An_invoice_may_be_due_on_the_day_it_is_issued()
    {
        Invoice.Issue(InvoiceId.New(), "2026-0042", IssuedOn, IssuedOn, "EUR", [Line("Toner", 1, 12.50m)]).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2026/0042")]
    [InlineData("2026 0042")]
    [InlineData("Rechnung-ä")]
    [InlineData("123456789012345678901234567890123")]
    public void An_invoice_number_other_than_one_to_32_letters_digits_or_hyphens_is_refused(string number)
    {
        var result = Invoice.Issue(InvoiceId.New(), number, IssuedOn, DueOn, "EUR", [Line("Toner", 1, 12.50m)]);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidInvoiceNumber>().Code.ShouldBe("billing.invalid-invoice-number");
    }

    [Theory]
    [InlineData("")]
    [InlineData("eur")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void A_currency_other_than_three_capital_letters_is_refused(string currency)
    {
        var result = Invoice.Issue(InvoiceId.New(), "2026-0042", IssuedOn, DueOn, currency, [Line("Toner", 1, 12.50m)]);

        result.Error.ShouldBeOfType<InvalidCurrency>().Code.ShouldBe("billing.invalid-currency");
    }

    [Fact]
    public void An_invoice_due_before_it_is_issued_is_refused()
    {
        var result = Invoice.Issue(InvoiceId.New(), "2026-0042", IssuedOn, IssuedOn.AddDays(-1), "EUR", [Line("Toner", 1, 12.50m)]);

        result.Error.ShouldBeOfType<DueBeforeIssued>().Code.ShouldBe("billing.due-before-issued");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void An_invoice_without_lines_or_with_more_than_the_most_lines_is_refused(int count)
    {
        List<InvoiceLine> lines = [.. Enumerable.Range(1, count).Select(number => Line($"Item {number}", 1, 1m))];

        var result = Invoice.Issue(InvoiceId.New(), "2026-0042", IssuedOn, DueOn, "EUR", lines);

        result.Error.ShouldBeOfType<InvalidLineCount>().Code.ShouldBe("billing.invalid-line-count");
    }

    [Fact]
    public void An_invoice_with_the_most_lines_is_issued()
    {
        List<InvoiceLine> lines = [.. Enumerable.Range(1, Invoice.MaximumLines).Select(number => Line($"Item {number}", 1, 1m))];

        var invoice = Invoice.Issue(InvoiceId.New(), "2026-0042", IssuedOn, DueOn, "EUR", lines).Value;

        invoice.Lines.Count.ShouldBe(Invoice.MaximumLines);
        invoice.Total.ShouldBe(Invoice.MaximumLines);
    }

    [Fact]
    public void A_line_is_described_with_its_trimmed_description_quantity_and_unit_price()
    {
        var line = InvoiceLine.Describe("  Toner  ", 2, 12.50m).Value;

        line.Description.ShouldBe("Toner");
        line.Quantity.ShouldBe(2);
        line.UnitPrice.ShouldBe(12.50m);
        line.Amount.ShouldBe(25.00m);
    }

    [Theory]
    [InlineData("", 1, "1")]
    [InlineData("   ", 1, "1")]
    [InlineData("Toner", 0, "1")]
    [InlineData("Toner", 10000, "1")]
    [InlineData("Toner", 1, "-0.01")]
    [InlineData("Toner", 1, "0.001")]
    [InlineData("Toner", 1, "10000000")]
    public void A_line_without_a_description_a_quantity_from_1_to_9999_or_a_unit_price_in_whole_cents_up_to_the_most_is_refused(string description, int quantity, string unitPrice)
    {
        var result = InvoiceLine.Describe(description, quantity, decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidInvoiceLine>().Code.ShouldBe("billing.invalid-invoice-line");
    }

    [Fact]
    public void A_line_with_a_description_longer_than_the_most_is_refused()
    {
        var result = InvoiceLine.Describe(new string('x', InvoiceLine.MaximumDescriptionLength + 1), 1, 1m);

        result.Error.ShouldBeOfType<InvalidInvoiceLine>();
    }

    [Fact]
    public void A_line_at_the_edges_of_its_ranges_is_described()
    {
        InvoiceLine.Describe(new string('x', InvoiceLine.MaximumDescriptionLength), InvoiceLine.MaximumQuantity, InvoiceLine.MaximumUnitPrice).IsSuccess.ShouldBeTrue();
        InvoiceLine.Describe("Free sample", 1, 0m).IsSuccess.ShouldBeTrue();
    }

    private static InvoiceLine Line(string description, int quantity, decimal unitPrice) =>
        InvoiceLine.Describe(description, quantity, unitPrice).Value;
}
