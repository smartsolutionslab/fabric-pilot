using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

/// <summary>A request with an optional record whose schema holds references: they must still resolve in the tool's.</summary>
public sealed record MatchPair(Option<Pair> Pair) : IQuery<Result<bool, ApiError>>;
