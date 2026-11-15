using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Application.DTOs;
using FabricPilot.Modules.Billing.Application.Queries.ListInvoices;
using FabricPilot.Modules.Billing.Domain.Invoices;

namespace FabricPilot.Modules.Billing.Tests;

/// <summary>The invoices table of one tenant, in memory.</summary>
internal sealed class InMemoryInvoiceStore : IInvoiceStore
{
    public List<Invoice> Saved { get; } = [];

    public Task<PagedResult<InvoiceSummaryDto>> ListAsync(Sorting<InvoiceSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var items = Saved.AsQueryable()
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(InvoiceMappings.ToInvoiceSummaryDtoExpression());
        return Task.FromResult(new PagedResult<InvoiceSummaryDto>([.. items], Saved.Count, page.Value, pageSize.Value));
    }

    public Task<Option<Invoice>> ReadAsync(InvoiceId id, CancellationToken cancellationToken)
    {
        var invoice = Saved.SingleOrDefault(candidate => candidate.Id == id);
        return Task.FromResult(invoice is null ? Option<Invoice>.None : Option<Invoice>.Some(invoice));
    }
}
