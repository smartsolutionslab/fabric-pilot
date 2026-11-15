using System.Linq.Expressions;
using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Application.DTOs;

public static class InvoiceMappings
{
    /// <summary>For an EF <c>Select</c>: the total is the one the domain stored, never summed here.</summary>
    public static Expression<Func<Invoice, InvoiceSummaryDto>> ToInvoiceSummaryDtoExpression() =>
        invoice => new InvoiceSummaryDto(invoice.Id.Value, invoice.Number, invoice.IssuedOn, invoice.DueOn, invoice.Currency, invoice.Total);
}
