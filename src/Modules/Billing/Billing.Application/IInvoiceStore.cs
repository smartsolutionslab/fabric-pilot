using FabricPilot.Modules.Billing.Application.DTOs;
using FabricPilot.Modules.Billing.Application.Queries.ListInvoices;
using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Application;

/// <summary>The invoices table, the current tenant's rows only; implemented in the module's Infrastructure project.</summary>
public interface IInvoiceStore
{
    /// <summary>One page of invoice summaries in the order asked for, projected in the database.</summary>
    Task<PagedResult<InvoiceSummaryDto>> ListAsync(Sorting<InvoiceSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken);

    /// <summary>The invoice with its lines as it is stored, for a query: nothing that changes it is saved.</summary>
    Task<Option<Invoice>> ReadAsync(InvoiceId id, CancellationToken cancellationToken);
}
