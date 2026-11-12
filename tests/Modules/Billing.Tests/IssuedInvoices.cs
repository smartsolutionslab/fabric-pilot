using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Tests;

/// <summary>Invoices issued for the tests: no use case issues one yet.</summary>
internal static class IssuedInvoices
{
    public static Invoice Issued(string number, DateOnly issuedOn, params (string Description, int Quantity, decimal UnitPrice)[] lines)
    {
        List<InvoiceLine> described = [.. lines.Select(line => InvoiceLine.Describe(line.Description, line.Quantity, line.UnitPrice).Value)];
        return Invoice.Issue(InvoiceId.New(), number, issuedOn, issuedOn.AddDays(30), "EUR", described).Value;
    }
}
