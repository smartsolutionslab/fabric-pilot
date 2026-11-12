using FabricPilot.BuildingBlocks.Messaging;

namespace FabricPilot.Agents.Tests;

/// <summary>The query that runs the sample agent, as a module's would: an endpoint sends it and never runs the agent itself.</summary>
public sealed record AskAboutWords(string Question) : IQuery<Result<string, ApiError>>;
