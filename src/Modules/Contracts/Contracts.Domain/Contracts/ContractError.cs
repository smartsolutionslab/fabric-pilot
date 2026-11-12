namespace FabricPilot.Modules.Contracts.Domain.Contracts;

/// <summary>The ways concluding a contract fails: a new one fails the build until the Application maps it.</summary>
public union ContractError(InvalidProduct, EndsBeforeItStarts);
