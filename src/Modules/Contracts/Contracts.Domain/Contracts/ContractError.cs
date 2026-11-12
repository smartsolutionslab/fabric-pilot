namespace FabricPilot.Modules.Contracts.Domain.Contracts;

/// <summary>The ways concluding a contract fails. No use case concludes one yet, so nothing maps them to an ApiError.</summary>
public union ContractError(InvalidProduct, EndsBeforeItStarts);
