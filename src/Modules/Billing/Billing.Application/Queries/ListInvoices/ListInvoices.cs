using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application.DTOs;

namespace FabricPilot.Modules.Billing.Application.Queries.ListInvoices;

/// <summary>One page of the caller's tenant's invoices, in the order asked for.</summary>
public sealed record ListInvoices(Page Page, PageSize PageSize, Sorting<InvoiceSortField> Sorting)
    : IQuery<Result<PagedResult<InvoiceSummaryDto>, ApiError>>
{
    /// <summary>The order the list has when the caller asks for none: the newest invoice first.</summary>
    public static Sorting<InvoiceSortField> NewestFirst { get; } = new(InvoiceSortField.IssuedOn, SortDirection.Descending);
}
