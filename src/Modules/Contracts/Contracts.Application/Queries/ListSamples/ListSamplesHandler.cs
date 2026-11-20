using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;

namespace FabricPilot.Modules.Contracts.Application.Queries.ListSamples;

public sealed class ListSamplesHandler(ISampleStore samples)
    : IQueryHandler<ListSamples, Result<PagedResult<ContractsSampleDto>, ApiError>>
{
    public async Task<Result<PagedResult<ContractsSampleDto>, ApiError>> HandleAsync(ListSamples query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var page = await samples.ListAsync(query.Page, query.PageSize, cancellationToken);
        var dtos = new PagedResult<ContractsSampleDto>([.. page.Items.Select(sample => sample.ToDto())], page.TotalCount, page.Page, page.PageSize);
        return Success(dtos);
    }
}
