using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Identity.Application.DTOs;
using FabricPilot.Modules.Identity.Domain.Samples;

namespace FabricPilot.Modules.Identity.Application.Queries.GetSample;

public sealed class GetSampleHandler(ISampleStore samples)
    : IQueryHandler<GetSample, Result<IdentitySampleDto, ApiError>>
{
    public async Task<Result<IdentitySampleDto, ApiError>> HandleAsync(GetSample query, CancellationToken cancellationToken)
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
