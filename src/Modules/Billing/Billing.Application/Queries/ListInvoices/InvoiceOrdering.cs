using System.Diagnostics;
using System.Linq.Expressions;
using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Application.Queries.ListInvoices;

public static class InvoiceOrdering
{
    /// <summary>Sorted as asked, then by id in the same direction, so invoices with the same key keep one order across pages.</summary>
    public static IOrderedQueryable<Invoice> SortedBy(this IQueryable<Invoice> invoices, Sorting<InvoiceSortField> sorting)
    {
        Ensure.That(invoices).IsNotNull();
        Ensure.That(sorting).IsNotNull();

        return sorting.Field switch
        {
            InvoiceSortField.IssuedOn => By(invoices, invoice => invoice.IssuedOn, sorting.Direction),
            InvoiceSortField.DueOn => By(invoices, invoice => invoice.DueOn, sorting.Direction),
            InvoiceSortField.Number => By(invoices, invoice => invoice.Number, sorting.Direction),
            InvoiceSortField.Total => By(invoices, invoice => invoice.Total, sorting.Direction),
            _ => throw new UnreachableException($"The list query refuses the sort field {sorting.Field}."),
        };
    }

    private static IOrderedQueryable<Invoice> By<TKey>(IQueryable<Invoice> invoices, Expression<Func<Invoice, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? invoices.OrderBy(key).ThenBy(invoice => invoice.Id)
            : invoices.OrderByDescending(key).ThenByDescending(invoice => invoice.Id);
}
