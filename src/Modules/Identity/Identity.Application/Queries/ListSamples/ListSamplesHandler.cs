using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;

namespace FabricPilot.Modules.Identity.Application.Queries.ListSamples;

public sealed class ListSamplesHandler(ISampleStore samples)
    : IQueryHandler<ListSamples, Result<PagedResult<IdentitySampleDto>, ApiError>>
{
    public async Task<Result<PagedResult<IdentitySampleDto>, ApiError>> HandleAsync(ListSamples query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var page = await samples.ListAsync(query.Page, query.PageSize, cancellationToken);
        var dtos = new PagedResult<IdentitySampleDto>([.. page.Items.Select(sample => sample.ToDto())], page.TotalCount, page.Page, page.PageSize);
        return Success(dtos);
    }
}
