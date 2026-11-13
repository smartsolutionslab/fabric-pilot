using FabricPilot.BuildingBlocks.Api;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application;
using FabricPilot.Modules.Billing.Application.DTOs;
using FabricPilot.Modules.Billing.Application.Queries.DownloadInvoicePdf;
using FabricPilot.Modules.Billing.Application.Queries.ListInvoices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FabricPilot.Modules.Billing.Api.Endpoints;

/// <summary>The caller's tenant's invoices: the list, and each one as a PDF.</summary>
public sealed class InvoiceEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        Ensure.That(endpoints).IsNotNull();

        var invoices = endpoints.MapGroup("/invoices").WithTags("Billing");
        invoices.MapGet(string.Empty, ListAsync)
            .RequireAuthorization(BillingPolicyNames.ReadInvoices);
        invoices.MapGet("/{invoiceId:guid}/pdf", async (Guid invoiceId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            (await dispatcher.QueryAsync(new DownloadInvoicePdf(invoiceId), cancellationToken))
                .ToHttpResult(pdf => TypedResults.File(pdf.Content, "application/pdf", pdf.FileName)))
            .Produces<Stream>(StatusCodes.Status200OK, "application/pdf")
            .RequireAuthorization(BillingPolicyNames.ReadInvoices);
    }

    /// <summary>Newest first unless the caller names a field; a named field sorts descending unless the caller says otherwise.</summary>
    private static async Task<Results<Ok<PagedResult<InvoiceSummaryDto>>, ProblemHttpResult>> ListAsync(
        Page? page,
        PageSize? pageSize,
        InvoiceSortField? sortBy,
        SortDirection? sortDirection,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var sorting = new Sorting<InvoiceSortField>(
            sortBy ?? ListInvoices.NewestFirst.Field,
            sortDirection ?? ListInvoices.NewestFirst.Direction);
        var listInvoices = new ListInvoices(page ?? Page.First, pageSize ?? PageSize.Default, sorting);
        return (await dispatcher.QueryAsync(listInvoices, cancellationToken)).ToHttpResult();
    }
}
