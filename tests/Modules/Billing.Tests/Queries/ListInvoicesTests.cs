using FabricPilot.Modules.Billing.Application.Queries.ListInvoices;
using SortDirection = FabricPilot.Kernel.SortDirection;

namespace FabricPilot.Modules.Billing.Tests.Queries;

public class ListInvoicesTests
{
    [Fact]
    public async Task The_list_shows_one_page_of_invoices_the_newest_first_with_their_dates_currency_and_total()
    {
        var store = new InMemoryInvoiceStore();
        var older = IssuedInvoices.Issued("2026-0041", new DateOnly(2026, 10, 1), ("Toner", 2, 12.50m));
        var newer = IssuedInvoices.Issued("2026-0042", new DateOnly(2026, 11, 1), ("Managed printing", 3, 49.90m), ("Toner", 2, 12.50m));
        store.Saved.AddRange([older, newer]);
        PageSize.TryFrom(1, out var one).ShouldBeTrue();

        var result = await new ListInvoicesHandler(store).HandleAsync(new ListInvoices(Page.First, one, ListInvoices.NewestFirst), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);
        var summary = result.Value.Items.ShouldHaveSingleItem();
        summary.Id.ShouldBe(newer.Id.Value);
        summary.Number.ShouldBe("2026-0042");
        summary.IssuedOn.ShouldBe(new DateOnly(2026, 11, 1));
        summary.DueOn.ShouldBe(new DateOnly(2026, 12, 1));
        summary.Currency.ShouldBe("EUR");
        summary.Total.ShouldBe(174.70m);
    }

    [Fact]
    public void The_newest_invoice_comes_first_by_default()
    {
        ListInvoices.NewestFirst.ShouldBe(new Sorting<InvoiceSortField>(InvoiceSortField.IssuedOn, SortDirection.Descending));
    }

    [Theory]
    [InlineData(InvoiceSortField.Number, SortDirection.Ascending, new[] { "A-1", "B-2", "C-3" })]
    [InlineData(InvoiceSortField.Number, SortDirection.Descending, new[] { "C-3", "B-2", "A-1" })]
    [InlineData(InvoiceSortField.Total, SortDirection.Ascending, new[] { "B-2", "C-3", "A-1" })]
    [InlineData(InvoiceSortField.DueOn, SortDirection.Ascending, new[] { "C-3", "A-1", "B-2" })]
    [InlineData(InvoiceSortField.IssuedOn, SortDirection.Ascending, new[] { "C-3", "A-1", "B-2" })]
    public async Task The_list_is_sorted_by_the_field_and_direction_asked_for(InvoiceSortField field, SortDirection direction, string[] expected)
    {
        var store = new InMemoryInvoiceStore();
        store.Saved.AddRange([
            IssuedInvoices.Issued("A-1", new DateOnly(2026, 2, 1), ("Hosting", 1, 300m)),
            IssuedInvoices.Issued("B-2", new DateOnly(2026, 3, 1), ("Toner", 1, 100m)),
            IssuedInvoices.Issued("C-3", new DateOnly(2026, 1, 1), ("Paper", 1, 200m)),
        ]);

        var result = await new ListInvoicesHandler(store).HandleAsync(new ListInvoices(Page.First, PageSize.Default, new Sorting<InvoiceSortField>(field, direction)), TestContext.Current.CancellationToken);

        result.Value.Items.Select(item => item.Number).ShouldBe(expected);
    }

    [Theory]
    [InlineData(99, SortDirection.Ascending)]
    [InlineData((int)InvoiceSortField.Number, (SortDirection)7)]
    public async Task A_sort_field_or_direction_the_query_does_not_declare_is_refused(int field, SortDirection direction)
    {
        var sorting = new Sorting<InvoiceSortField>((InvoiceSortField)field, direction);

        await Should.ThrowAsync<GuardException>(() =>
            new ListInvoicesHandler(new InMemoryInvoiceStore()).HandleAsync(new ListInvoices(Page.First, PageSize.Default, sorting), TestContext.Current.CancellationToken));
    }
}
