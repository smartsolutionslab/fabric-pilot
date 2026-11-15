using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Application.DTOs;
using FabricPilot.Modules.Billing.Application.Queries.ListInvoices;
using FabricPilot.Modules.Billing.Domain.Invoices;
using FabricPilot.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Billing.Infrastructure;

/// <summary>Every read is filtered to the current tenant by the DbContext's query filter.</summary>
public sealed class InvoiceStore(BillingDbContext context) : IInvoiceStore
{
    public async Task<PagedResult<InvoiceSummaryDto>> ListAsync(Sorting<InvoiceSortField> sorting, Page page, PageSize pageSize, CancellationToken cancellationToken)
    {
        Ensure.That(sorting).IsNotNull();
        Ensure.That(page).IsNotNull();
        Ensure.That(pageSize).IsNotNull();

        var invoices = context.Set<Invoice>().AsNoTracking();
        var total = await invoices.CountAsync(cancellationToken);
        var items = await invoices
            .SortedBy(sorting)
            .Skip(page.Skip(pageSize))
            .Take(pageSize.Value)
            .Select(InvoiceMappings.ToInvoiceSummaryDtoExpression())
            .ToListAsync(cancellationToken);
        return new PagedResult<InvoiceSummaryDto>(items, total, page.Value, pageSize.Value);
    }

    public async Task<Option<Invoice>> ReadAsync(InvoiceId id, CancellationToken cancellationToken)
    {
        Ensure.That(id).IsNotNull();

        var invoice = await context.Set<Invoice>().AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return invoice is null ? Option<Invoice>.None : Option<Invoice>.Some(invoice);
    }
}
