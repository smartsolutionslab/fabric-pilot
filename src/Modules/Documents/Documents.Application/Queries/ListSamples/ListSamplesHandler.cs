using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Documents.Application.DTOs;

namespace FabricPilot.Modules.Documents.Application.Queries.ListSamples;

public sealed class ListSamplesHandler(ISampleStore samples)
    : IQueryHandler<ListSamples, Result<PagedResult<DocumentsSampleDto>, ApiError>>
{
    public async Task<Result<PagedResult<DocumentsSampleDto>, ApiError>> HandleAsync(ListSamples query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        var page = await samples.ListAsync(query.Page, query.PageSize, cancellationToken);
        var dtos = new PagedResult<DocumentsSampleDto>([.. page.Items.Select(sample => sample.ToDto())], page.TotalCount, page.Page, page.PageSize);
        return Success(dtos);
    }
}
