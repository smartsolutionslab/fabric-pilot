using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Documents.Domain.Samples;

namespace FabricPilot.Modules.Documents.Application.Commands.CreateSample;

public sealed class CreateSampleHandler(ISampleStore samples, IDocumentsUnitOfWork unitOfWork)
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
