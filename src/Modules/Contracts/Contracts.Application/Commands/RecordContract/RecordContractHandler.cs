using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Application.Commands.RecordContract;

/// <summary>The renewal date is the domain's; the contract belongs to the tenant the request works in.</summary>
public sealed class RecordContractHandler(IContractStore contracts, IContractsUnitOfWork unitOfWork)
    : ICommandHandler<RecordContract, Result<Guid, ApiError>>
{
    public async Task<Result<Guid, ApiError>> HandleAsync(RecordContract command, CancellationToken cancellationToken)
    {
        Ensure.That(command).IsNotNull();

        var (product, startsOn, endsOn, renews) = command;
        var concluded = Contract.Conclude(ContractId.New(), product, startsOn, endsOn, renews ? ContractRenewal.Renews : ContractRenewal.Ends);
        if (concluded.IsFailure)
        {
            return Failure(ContractsFailures.From(concluded.Error));
        }

        contracts.Add(concluded.Value);
        await unitOfWork.CommitAsync(cancellationToken);
        return Success(concluded.Value.Id.Value);
    }
}
