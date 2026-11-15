namespace FabricPilot.Modules.Contracts.Domain.Contracts;

/// <summary>The ways concluding a contract fails; <c>ContractsFailures</c> maps each to an ApiError.</summary>
public union ContractError(InvalidProduct, EndsBeforeItStarts);
