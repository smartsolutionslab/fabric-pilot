using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application.DTOs;

namespace FabricPilot.Modules.Billing.Application.Queries.ListInvoices;

public sealed class ListInvoicesHandler(IInvoiceStore invoices) : IQueryHandler<ListInvoices, Result<PagedResult<InvoiceSummaryDto>, ApiError>>
{
    public async Task<Result<PagedResult<InvoiceSummaryDto>, ApiError>> HandleAsync(ListInvoices query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var (page, pageSize, sorting) = query;
        Ensure.That(sorting).IsNotNull();
        Ensure.That((int)sorting.Field, nameof(sorting)).Satisfies(field => Enum.IsDefined((InvoiceSortField)field), $"The invoice list cannot be sorted by {sorting.Field}.");
        Ensure.That((int)sorting.Direction, nameof(sorting)).Satisfies(direction => Enum.IsDefined((SortDirection)direction), $"{sorting.Direction} is not a sort direction.");

        return Success(await invoices.ListAsync(sorting, page, pageSize, cancellationToken));
    }
}
