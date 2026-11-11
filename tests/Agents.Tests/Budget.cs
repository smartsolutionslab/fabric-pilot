using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

public sealed record Budget(AmountCollection Amounts) : IQuery<Result<int, ApiError>>;
