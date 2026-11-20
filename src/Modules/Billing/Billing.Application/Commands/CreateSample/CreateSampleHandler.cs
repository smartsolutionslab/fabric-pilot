using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Billing.Domain.Samples;

namespace FabricPilot.Modules.Billing.Application.Commands.CreateSample;

public sealed class CreateSampleHandler(ISampleStore samples, IBillingUnitOfWork unitOfWork)
    : ICommandHandler<CreateSample, Result<Guid, ApiError>>
{
    public async Task<Result<Guid, ApiError>> HandleAsync(CreateSample command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var sample = Sample.Create(SampleId.New(), command.Name);
        samples.Add(sample);
        await unitOfWork.CommitAsync(cancellationToken);
        return Success(sample.Id.Value);
    }
}
