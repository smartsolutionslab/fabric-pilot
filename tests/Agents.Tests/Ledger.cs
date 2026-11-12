using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

/// <summary>A list of collections of absent values, the inner one by inheritance: no tool can describe it.</summary>
public sealed record Ledger(List<AmountCollection> Batches) : IQuery<Result<int, ApiError>>;
