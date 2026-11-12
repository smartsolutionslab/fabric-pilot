using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

/// <summary>A query whose argument's constructor refuses a blank label while the arguments are read.</summary>
public sealed record LabelShelf(ShelfLabel Label) : IQuery<Result<string, ApiError>>;
