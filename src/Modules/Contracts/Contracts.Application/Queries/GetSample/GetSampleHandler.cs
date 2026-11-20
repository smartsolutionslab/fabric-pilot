using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Application.DTOs;
using FabricPilot.Modules.Contracts.Domain.Samples;

namespace FabricPilot.Modules.Contracts.Application.Queries.GetSample;

public sealed class GetSampleHandler(ISampleStore samples)
    : IQueryHandler<GetSample, Result<ContractsSampleDto, ApiError>>
{
    public async Task<Result<ContractsSampleDto, ApiError>> HandleAsync(GetSample query, CancellationToken cancellationToken)
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
