using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application.DTOs;

namespace FabricPilot.Modules.Billing.Application.Queries.ListSamples;

public sealed class ListSamplesHandler(ISampleStore samples)
    : IQueryHandler<ListSamples, Result<PagedResult<BillingSampleDto>, ApiError>>
{
    public async Task<Result<PagedResult<BillingSampleDto>, ApiError>> HandleAsync(ListSamples query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var page = await samples.ListAsync(query.Page, query.PageSize, cancellationToken);
        var dtos = new PagedResult<BillingSampleDto>([.. page.Items.Select(sample => sample.ToDto())], page.TotalCount, page.Page, page.PageSize);
        return Success(dtos);
    }
}
