using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Modules.Contracts.Application.Commands.RecordContract;

/// <summary>
/// Staff record a contract for the tenant the request works in: its product, its first and last day, and whether a new
/// term starts the day after; answers the new contract's id.
/// </summary>
public sealed record RecordContract(string Product, DateOnly StartsOn, DateOnly EndsOn, bool Renews) : ICommand<Result<Guid, ApiError>>;
