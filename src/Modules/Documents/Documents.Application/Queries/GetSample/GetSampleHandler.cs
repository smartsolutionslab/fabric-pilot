using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Documents.Application.DTOs;
using FabricPilot.Modules.Documents.Domain.Samples;

namespace FabricPilot.Modules.Documents.Application.Queries.GetSample;

public sealed class GetSampleHandler(ISampleStore samples)
    : IQueryHandler<GetSample, Result<DocumentsSampleDto, ApiError>>
{
    public async Task<Result<DocumentsSampleDto, ApiError>> HandleAsync(GetSample query, CancellationToken cancellationToken)
    {
        Ensure.That(query).IsNotNull();

        if (query.Id == Guid.Empty)
        {
            return Failure(SampleFailures.NotFound(query.Id));
        }

        var sample = await samples.FindAsync(SampleId.From(query.Id), cancellationToken);
        if (!sample.HasValue)
        {
            return Failure(SampleFailures.NotFound(query.Id));
        }

        return Success(sample.Value.ToDto());
    }
}
