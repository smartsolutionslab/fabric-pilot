using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Application.DTOs;
using FabricPilot.Modules.Billing.Domain.Samples;

namespace FabricPilot.Modules.Billing.Application.Queries.GetSample;

public sealed class GetSampleHandler(ISampleStore samples)
    : IQueryHandler<GetSample, Result<BillingSampleDto, ApiError>>
{
    public async Task<Result<BillingSampleDto, ApiError>> HandleAsync(GetSample query, CancellationToken cancellationToken)
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
